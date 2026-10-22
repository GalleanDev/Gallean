using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace Gallean.Client;

internal sealed class AudioPlayer : IAudioPlayer
{
    private readonly IAssetProvider assets;
    private readonly ILog log;
    private readonly MixingSampleProvider sounds = CreateMixer();
    private readonly MixingSampleProvider music = CreateMixer();
    private readonly VolumeSampleProvider soundsVolume;
    private readonly VolumeSampleProvider musicVolume;
    private readonly AudioOutput output;

    public AudioPlayer(IAssetProvider assets, AudioOutput output, ILog log, float initialSoundVolume, float initialMusicVolume)
    {
        this.assets = assets;
        this.output = output;
        this.log = log;

        soundsVolume = new VolumeSampleProvider(sounds)
        {
            Volume = initialSoundVolume
        };
        musicVolume = new VolumeSampleProvider(music)
        {
            Volume = initialMusicVolume
        };

        output.AddInput(soundsVolume);
        output.AddInput(musicVolume);
    }

    public float SoundVolume
    {
        get => soundsVolume.Volume;
        set => soundsVolume.Volume = value;
    }

    public float MusicVolume
    {
        get => musicVolume.Volume;
        set => musicVolume.Volume = value;
    }

    public void PlaySound(AssetSoundKey key)
    {
        if (!output.IsOpen)
        {
            return;
        }

        using AssetCache<Sound>.Lease? lease = assets.GetOptionalSound(key);

        if (lease is null)
        {
            return;
        }

        Sound sound = lease;

        TryAddMixerInput(sounds, SoundSampleProvider.Single(sound, sounds.WaveFormat));
    }

    public IMusicPlayback? PlayMusic(MusicQueue queue, MusicPosition position)
    {
        if (!output.IsOpen)
        {
            return null;
        }

        MusicSampleProvider samples = new(new MusicQueueSampleProvider(assets, queue, position));

        if (TryAddMixerInput(music, samples))
        {
            return samples;
        }
        else
        {
            return null;
        }
    }

    public IAudioPlayback? PlayVoice(AssetSoundKey key)
    {
        if (!output.IsOpen)
        {
            return null;
        }

        MusicQueueSampleProvider samples = new(assets, MusicQueue.Create([key], isLooping: false), MusicPosition.Start);

        if (TryAddMixerInput(sounds, samples))
        {
            return samples;
        }
        else
        {
            return null;
        }
    }

    public IAmbientAudioPlayback? PlayAmbientSound(AssetSoundKey key, float volume, float pan)
    {
        if (!output.IsOpen)
        {
            return null;
        }

        MusicQueueSampleProvider loop = new(assets, MusicQueue.Create([key], isLooping: true), MusicPosition.Start);
        AmbientSoundSampleProvider samples = new(loop, volume, pan);

        if (TryAddMixerInput(sounds, samples))
        {
            return samples;
        }
        else
        {
            return null;
        }
    }

    private static MixingSampleProvider CreateMixer()
    {
        return new MixingSampleProvider(WaveFormat.CreateIeeeFloatWaveFormat(SoundDecoder.MixerSampleRate, SoundDecoder.MixerChannels))
        {
            ReadFully = true
        };
    }

    private bool TryAddMixerInput(MixingSampleProvider mixer, ISampleProvider input)
    {
        try
        {
            mixer.AddMixerInput(input);

            return true;
        }
        catch (Exception ex)
        {
            log.Error("Failed to add sound to the output.", ex);

            return false;
        }
    }
}
