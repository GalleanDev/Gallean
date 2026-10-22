using System.Buffers.Binary;
using System.Numerics;

namespace GameContent;

internal sealed partial class BinkAudioDecoder
{
    private const int MaxBandCount = 25;
    private const int QuantTableLength = 96;
    private const int QuantIndexSize = 8;
    private const int ReportedSizeBits = 32;
    private const int BlockHeaderBits = 58;
    private const float QuantTableStep = 0.15289164787221953823f;

    private static readonly int[] WmaCriticalFrequencies =
    [
        100, 200, 300, 400, 510, 630, 770, 920, 1080, 1270, 1480, 1720, 2000,
        2320, 2700, 3150, 3700, 4400, 5300, 6400, 7700, 9500, 12000, 15500, 24500
    ];

    private static readonly int[] RleLengthTable = [2, 3, 4, 5, 6, 8, 9, 10, 11, 12, 13, 14, 15, 16, 32, 64];

    private readonly int frameLength;
    private readonly int overlapLength;
    private readonly int blockSize;
    private readonly int bandCount;
    private readonly float root;
    private readonly int[] bands;
    private readonly float[] previous;
    private readonly float[] quantTable = new float[QuantTableLength];
    private readonly float[] quants = new float[MaxBandCount];
    private readonly float[] coefficients;
    private readonly float[] output;
    private readonly Complex[] transform;
    private readonly Complex[] twiddles;
    private readonly int[] bitReversal;
    private bool first = true;

    public BinkAudioDecoder(int sampleRate, int channels)
    {
        int frameLengthBits = sampleRate switch
        {
            < 22050 => 9,
            < 44100 => 10,
            _ => 11
        };

        int interleavedSampleRate = sampleRate * channels;
        frameLengthBits += BitOperations.Log2((uint)channels);

        frameLength = 1 << frameLengthBits;
        overlapLength = frameLength / 16;
        blockSize = frameLength - overlapLength;
        root = 2.0f / (MathF.Sqrt(frameLength) * 32768.0f);

        for (int index = 0; index < QuantTableLength; index++)
        {
            quantTable[index] = MathF.Exp(index * QuantTableStep) * root;
        }

        int sampleRateHalf = (interleavedSampleRate + 1) / 2;

        bandCount = 1;

        while (bandCount < MaxBandCount && sampleRateHalf > WmaCriticalFrequencies[bandCount - 1])
        {
            bandCount++;
        }

        bands = new int[bandCount + 1];
        bands[0] = 2;

        for (int index = 1; index < bandCount; index++)
        {
            bands[index] = (WmaCriticalFrequencies[index - 1] * frameLength / sampleRateHalf) & ~1;
        }

        bands[bandCount] = frameLength;
        previous = new float[overlapLength];
        coefficients = new float[frameLength + 2];
        output = new float[frameLength];
        transform = new Complex[frameLength];
        twiddles = CreateTwiddles(frameLength);
        bitReversal = CreateBitReversal(frameLengthBits);
    }

    public float[] DecodePacket(ReadOnlySpan<byte> packet)
    {
        if (packet.Length < sizeof(uint))
        {
            throw new InvalidDataException("Unexpected Bink audio packet.");
        }

        float[] samples = new float[checked((int)(BinaryPrimitives.ReadUInt32LittleEndian(packet) / sizeof(short)))];
        BitReader reader = new(packet);
        int written = 0;

        reader.SkipBits(ReportedSizeBits);

        while (reader.BitsLeft > 0)
        {
            DecodeBlock(ref reader);

            int count = Math.Clamp(samples.Length - written, 0, blockSize);

            output.AsSpan(0, count).CopyTo(samples.AsSpan(written));
            written += count;
            reader.Align32();
        }

        return samples;
    }

    private void DecodeBlock(ref BitReader reader)
    {
        if (reader.BitsLeft < BlockHeaderBits)
        {
            throw new InvalidDataException("Unexpected Bink audio packet.");
        }

        coefficients[0] = GetFloat(ref reader) * root;
        coefficients[1] = GetFloat(ref reader) * root;

        if (reader.BitsLeft < bandCount * QuantIndexSize)
        {
            throw new InvalidDataException("Unexpected Bink audio packet.");
        }

        for (int index = 0; index < bandCount; index++)
        {
            int value = reader.GetBits(QuantIndexSize);

            quants[index] = quantTable[Math.Min(value, QuantTableLength - 1)];
        }

        int band = 0;
        float quant = quants[0];
        int position = 2;

        while (position < frameLength)
        {
            int end;

            if (reader.GetBit() != 0)
            {
                end = position + (RleLengthTable[reader.GetBits(4)] * 8);
            }
            else
            {
                end = position + 8;
            }

            end = Math.Min(end, frameLength);

            int width = reader.GetBits(4);

            if (width == 0)
            {
                coefficients.AsSpan(position, end - position).Clear();
                position = end;

                while (bands[band] < position)
                {
                    quant = quants[band];
                    band++;
                }
            }
            else
            {
                while (position < end)
                {
                    if (bands[band] == position)
                    {
                        quant = quants[band];
                        band++;
                    }

                    int coefficient = reader.GetBits(width);

                    if (coefficient == 0)
                    {
                        coefficients[position] = 0;
                    }
                    else if (reader.GetBit() != 0)
                    {
                        coefficients[position] = -quant * coefficient;
                    }
                    else
                    {
                        coefficients[position] = quant * coefficient;
                    }

                    position++;
                }
            }
        }

        for (int index = 2; index < frameLength; index += 2)
        {
            coefficients[index + 1] *= -1;
        }

        coefficients[frameLength] = coefficients[1];
        coefficients[frameLength + 1] = 0;
        coefficients[1] = 0;

        InverseRdft();

        if (!first)
        {
            for (int index = 0; index < overlapLength; index++)
            {
                output[index] = ((previous[index] * (overlapLength - index)) + (output[index] * index)) / overlapLength;
            }
        }

        output.AsSpan(frameLength - overlapLength, overlapLength).CopyTo(previous);
        first = false;
    }

    private void InverseRdft()
    {
        int half = frameLength / 2;

        for (int bin = 0; bin <= half; bin++)
        {
            transform[bin] = new Complex(coefficients[2 * bin], coefficients[(2 * bin) + 1]);
        }

        for (int bin = 1; bin < half; bin++)
        {
            transform[frameLength - bin] = Complex.Conjugate(transform[bin]);
        }

        for (int index = 0; index < frameLength; index++)
        {
            int reversed = bitReversal[index];

            if (index < reversed)
            {
                (transform[index], transform[reversed]) = (transform[reversed], transform[index]);
            }
        }

        for (int size = 2; size <= frameLength; size *= 2)
        {
            int halfSize = size / 2;
            int step = frameLength / size;

            for (int start = 0; start < frameLength; start += size)
            {
                for (int offset = 0; offset < halfSize; offset++)
                {
                    Complex product = transform[start + offset + halfSize] * twiddles[offset * step];
                    Complex even = transform[start + offset];

                    transform[start + offset] = even + product;
                    transform[start + offset + halfSize] = even - product;
                }
            }
        }

        for (int index = 0; index < frameLength; index++)
        {
            output[index] = (float)(transform[index].Real * 0.5);
        }
    }

    private static float GetFloat(ref BitReader reader)
    {
        int power = reader.GetBits(5);
        float value = MathF.ScaleB(reader.GetBits(23), power - 23);

        if (reader.GetBit() != 0)
        {
            return -value;
        }
        else
        {
            return value;
        }
    }

    private static Complex[] CreateTwiddles(int length)
    {
        Complex[] result = new Complex[length / 2];

        for (int index = 0; index < result.Length; index++)
        {
            result[index] = Complex.FromPolarCoordinates(1, 2 * Math.PI * index / length);
        }

        return result;
    }

    private static int[] CreateBitReversal(int bits)
    {
        int[] result = new int[1 << bits];

        for (int index = 0; index < result.Length; index++)
        {
            result[index] = ReverseBits(index, bits);
        }

        return result;
    }

    private static int ReverseBits(int value, int bits)
    {
        int result = 0;

        for (int bit = 0; bit < bits; bit++)
        {
            result = (result << 1) | ((value >> bit) & 1);
        }

        return result;
    }
}
