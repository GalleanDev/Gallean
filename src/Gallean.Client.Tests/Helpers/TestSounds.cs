using OggVorbisEncoder;

namespace Gallean.Client.Tests;

internal static class TestSounds
{
    private const int PrimingFrames = 1024;
    private const float Quality = 0.0f;

    public static byte[] Create(int sampleRate, int channels, int frameCount)
    {
        float[][] planes = new float[channels][];
        float[][] priming = new float[channels][];

        for (int channel = 0; channel < channels; channel++)
        {
            planes[channel] = new float[frameCount];
            priming[channel] = new float[PrimingFrames];
        }

        VorbisInfo info = VorbisInfo.InitVariableBitRate(channels, sampleRate, Quality);
        OggStream stream = new(1);
        MemoryStream output = new();

        stream.PacketIn(HeaderPacketBuilder.BuildInfoPacket(info));
        stream.PacketIn(HeaderPacketBuilder.BuildCommentsPacket(new Comments()));
        stream.PacketIn(HeaderPacketBuilder.BuildBooksPacket(info));
        WritePages(stream, output, true);

        ProcessingState state = ProcessingState.Create(info);
        state.WriteData(priming, PrimingFrames);
        state.WriteData(planes, frameCount);
        state.WriteEndOfStream();

        while (state.PacketOut(out OggPacket packet))
        {
            stream.PacketIn(packet);
            WritePages(stream, output, false);
        }

        WritePages(stream, output, true);

        return output.ToArray();
    }

    private static void WritePages(OggStream stream, MemoryStream output, bool force)
    {
        while (stream.PageOut(out OggPage page, force))
        {
            output.Write(page.Header);
            output.Write(page.Body);
        }
    }
}
