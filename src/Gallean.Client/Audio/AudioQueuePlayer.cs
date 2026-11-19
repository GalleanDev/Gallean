using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using NAudio.Wave;

namespace Gallean.Client;

/// <summary>
/// Plays 32-bit float samples through an Apple Audio Queue.
/// </summary>
/// <remarks>
/// Core Audio HAL players run managed code on the real-time I/O thread, where a garbage collection or a slow
/// decode blocks the device for longer than its ~12 ms cycle and produces clicks. An Audio Queue keeps several
/// buffers queued ahead and calls back on its own non-real-time thread, so such pauses are absorbed.
/// The queue follows the system default output device.
/// </remarks>
[SupportedOSPlatform("macos")]
internal sealed unsafe partial class AudioQueuePlayer : IWavePlayer
{
    private const string AudioToolbox = "/System/Library/Frameworks/AudioToolbox.framework/AudioToolbox";
    private const uint LinearPcmFormat = 0x6C70636D; // 'lpcm'
    private const uint FloatPackedFlags = 0x1 | 0x8; // kAudioFormatFlagIsFloat | kAudioFormatFlagIsPacked
    private const uint VolumeParameter = 1; // kAudioQueueParam_Volume

    private readonly int bufferCount;
    private readonly int bufferFrames;
    private readonly Lock locker = new();
    private GCHandle handle;
    private nint queue;
    private ISampleProvider? source;
    private volatile Exception? failure;
    private float volume = 1;

    public AudioQueuePlayer(int bufferCount = 4, int bufferFrames = 1024)
    {
        this.bufferCount = bufferCount;
        this.bufferFrames = bufferFrames;
    }

    public event EventHandler<StoppedEventArgs>? PlaybackStopped;

    public PlaybackState PlaybackState { get; private set; } = PlaybackState.Stopped;

    public WaveFormat OutputWaveFormat => source?.WaveFormat ?? throw new InvalidOperationException("Player not yet initialized.");

    public float Volume
    {
        get => volume;
        set
        {
            volume = value;

            lock (locker)
            {
                if (queue != 0)
                {
                    ThrowIfFailed(AudioQueueSetParameter(queue, VolumeParameter, value), nameof(AudioQueueSetParameter));
                }
            }
        }
    }

    public void Init(IWaveProvider waveProvider)
    {
        ArgumentNullException.ThrowIfNull(waveProvider);

        WaveFormat format = waveProvider.WaveFormat;

        if (format.Encoding != WaveFormatEncoding.IeeeFloat || format.BitsPerSample != 32)
        {
            throw new ArgumentException("Only 32-bit float samples are supported.", nameof(waveProvider));
        }

        lock (locker)
        {
            if (queue != 0)
            {
                throw new InvalidOperationException("This player has already been initialized.");
            }

            source = waveProvider.ToSampleProvider();

            uint bytesPerFrame = (uint)(format.Channels * sizeof(float));
            AudioStreamBasicDescription description = new()
            {
                SampleRate = format.SampleRate,
                FormatId = LinearPcmFormat,
                FormatFlags = FloatPackedFlags,
                BytesPerPacket = bytesPerFrame,
                FramesPerPacket = 1,
                BytesPerFrame = bytesPerFrame,
                ChannelsPerFrame = (uint)format.Channels,
                BitsPerChannel = 32
            };

            handle = GCHandle.Alloc(this);

            nint created;
            ThrowIfFailed(
                AudioQueueNewOutput(&description, &OnBufferCompleted, GCHandle.ToIntPtr(handle), 0, 0, 0, &created),
                nameof(AudioQueueNewOutput));
            queue = created;

            ThrowIfFailed(AudioQueueSetParameter(queue, VolumeParameter, volume), nameof(AudioQueueSetParameter));

            for (int i = 0; i < bufferCount; i++)
            {
                AudioQueueBuffer* buffer;
                ThrowIfFailed(AudioQueueAllocateBuffer(queue, (uint)bufferFrames * bytesPerFrame, &buffer), nameof(AudioQueueAllocateBuffer));

                Fill(buffer);
                ThrowIfFailed(AudioQueueEnqueueBuffer(queue, buffer, 0, null), nameof(AudioQueueEnqueueBuffer));
            }
        }
    }

    public void Play()
    {
        lock (locker)
        {
            ThrowIfNotInitialized();
            ThrowIfFailed(AudioQueueStart(queue, null), nameof(AudioQueueStart));
            PlaybackState = PlaybackState.Playing;
        }
    }

    public void Pause()
    {
        lock (locker)
        {
            ThrowIfNotInitialized();
            ThrowIfFailed(AudioQueuePause(queue), nameof(AudioQueuePause));
            PlaybackState = PlaybackState.Paused;
        }
    }

    public void Stop()
    {
        StopCore(null);
    }

    public void Dispose()
    {
        lock (locker)
        {
            if (queue != 0)
            {
                // Immediate disposal waits for a running callback and guarantees no further ones.
                _ = AudioQueueDispose(queue, immediate: 1);
                queue = 0;
            }

            if (handle.IsAllocated)
            {
                handle.Free();
            }

            PlaybackState = PlaybackState.Stopped;
        }
    }

    private void StopCore(Exception? exception)
    {
        lock (locker)
        {
            if (queue == 0 || PlaybackState == PlaybackState.Stopped)
            {
                return;
            }

            _ = AudioQueueStop(queue, immediate: 1);
            PlaybackState = PlaybackState.Stopped;
        }

        PlaybackStopped?.Invoke(this, new StoppedEventArgs(exception));
    }

    private void Fill(AudioQueueBuffer* buffer)
    {
        Span<float> samples = new(buffer->AudioData, (int)(buffer->AudioDataBytesCapacity / sizeof(float)));
        int read = 0;

        if (failure is null && source is not null)
        {
            try
            {
                read = source.Read(samples);
            }
            catch (Exception ex)
            {
                failure = ex;

                // The queue must not be stopped from its own callback.
                ThreadPool.UnsafeQueueUserWorkItem(static player => player.StopCore(player.failure), this, preferLocal: false);
            }
        }

        samples[Math.Max(read, 0)..].Clear();
        buffer->AudioDataByteSize = buffer->AudioDataBytesCapacity;
    }

    [UnmanagedCallersOnly]
    private static void OnBufferCompleted(nint userData, nint queue, AudioQueueBuffer* buffer)
    {
        if (GCHandle.FromIntPtr(userData).Target is not AudioQueuePlayer player)
        {
            return;
        }

        player.Fill(buffer);
        _ = AudioQueueEnqueueBuffer(queue, buffer, 0, null);
    }

    private void ThrowIfNotInitialized()
    {
        if (queue == 0)
        {
            throw new InvalidOperationException("Player not yet initialized.");
        }
    }

    private static void ThrowIfFailed(int status, string function)
    {
        if (status != 0)
        {
            throw new InvalidOperationException($"{function} failed with OSStatus {status}.");
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct AudioStreamBasicDescription
    {
        public double SampleRate;
        public uint FormatId;
        public uint FormatFlags;
        public uint BytesPerPacket;
        public uint FramesPerPacket;
        public uint BytesPerFrame;
        public uint ChannelsPerFrame;
        public uint BitsPerChannel;
        public uint Reserved;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct AudioQueueBuffer
    {
        public uint AudioDataBytesCapacity;
        public void* AudioData;
        public uint AudioDataByteSize;
        public void* UserData;
        public uint PacketDescriptionCapacity;
        public void* PacketDescriptions;
        public uint PacketDescriptionCount;
    }

    [LibraryImport(AudioToolbox)]
    private static partial int AudioQueueNewOutput(
        AudioStreamBasicDescription* format,
        delegate* unmanaged<nint, nint, AudioQueueBuffer*, void> callback,
        nint userData,
        nint runLoop,
        nint runLoopMode,
        uint flags,
        nint* queue);

    [LibraryImport(AudioToolbox)]
    private static partial int AudioQueueAllocateBuffer(nint queue, uint byteSize, AudioQueueBuffer** buffer);

    [LibraryImport(AudioToolbox)]
    private static partial int AudioQueueEnqueueBuffer(nint queue, AudioQueueBuffer* buffer, uint packetDescriptionCount, void* packetDescriptions);

    [LibraryImport(AudioToolbox)]
    private static partial int AudioQueueStart(nint queue, void* startTime);

    [LibraryImport(AudioToolbox)]
    private static partial int AudioQueuePause(nint queue);

    [LibraryImport(AudioToolbox)]
    private static partial int AudioQueueStop(nint queue, byte immediate);

    [LibraryImport(AudioToolbox)]
    private static partial int AudioQueueDispose(nint queue, byte immediate);

    [LibraryImport(AudioToolbox)]
    private static partial int AudioQueueSetParameter(nint queue, uint parameter, float value);
}
