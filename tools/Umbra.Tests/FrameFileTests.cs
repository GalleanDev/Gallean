using Gallean.Tests.Common;
using TorchSharp;

namespace Umbra.Tests;

public sealed class FrameFileTests
{
    private static float[] ReadValues(FrameFile frames, int index)
    {
        using torch.Tensor frame = frames.Read(index);

        return frame.contiguous().data<float>().ToArray();
    }

    private static torch.Tensor MakeFrame(float[] values)
    {
        return torch.tensor(values, [1, 1, 2, 2], torch.ScalarType.Float32);
    }

    [Test]
    public void RoundTripTest()
    {
        using Sandbox sb = Sandbox.Create();

        using FrameFile frames = new(3, [1, 1, 2, 2], torch.ScalarType.Float32, sb.GetPath("frames.bin"));
        using torch.Tensor first = MakeFrame([1.5f, -2.25f, 0.0f, 7.125f]);
        using torch.Tensor second = MakeFrame([3.0f, 4.0f, 5.0f, 6.0f]);
        using torch.Tensor third = MakeFrame([-1.0f, -2.0f, -3.0f, -4.0f]);

        frames.Write(0, first);
        frames.Write(1, second);
        frames.Write(2, third);

        Assert.That(frames.Count, Is.EqualTo(3));
        Assert.That(ReadValues(frames, 0), Is.EqualTo([1.5f, -2.25f, 0.0f, 7.125f]));
        Assert.That(ReadValues(frames, 1), Is.EqualTo([3.0f, 4.0f, 5.0f, 6.0f]));
        Assert.That(ReadValues(frames, 2), Is.EqualTo([-1.0f, -2.0f, -3.0f, -4.0f]));
    }

    [Test]
    public void RereadTest()
    {
        using Sandbox sb = Sandbox.Create();

        using FrameFile frames = new(1, [1, 1, 2, 2], torch.ScalarType.Float32, sb.GetPath("frames.bin"));
        using torch.Tensor frame = MakeFrame([9.0f, 8.0f, 7.0f, 6.0f]);

        frames.Write(0, frame);

        Assert.That(ReadValues(frames, 0), Is.EqualTo([9.0f, 8.0f, 7.0f, 6.0f]));
        Assert.That(ReadValues(frames, 0), Is.EqualTo([9.0f, 8.0f, 7.0f, 6.0f]));
    }

    [Test]
    public void OutOfOrderWriteTest()
    {
        using Sandbox sb = Sandbox.Create();

        using FrameFile frames = new(3, [1, 1, 2, 2], torch.ScalarType.Float32, sb.GetPath("frames.bin"));
        using torch.Tensor third = MakeFrame([30.0f, 31.0f, 32.0f, 33.0f]);
        using torch.Tensor first = MakeFrame([10.0f, 11.0f, 12.0f, 13.0f]);
        using torch.Tensor second = MakeFrame([20.0f, 21.0f, 22.0f, 23.0f]);

        frames.Write(2, third);
        frames.Write(0, first);
        frames.Write(1, second);

        Assert.That(ReadValues(frames, 0), Is.EqualTo([10.0f, 11.0f, 12.0f, 13.0f]));
        Assert.That(ReadValues(frames, 1), Is.EqualTo([20.0f, 21.0f, 22.0f, 23.0f]));
        Assert.That(ReadValues(frames, 2), Is.EqualTo([30.0f, 31.0f, 32.0f, 33.0f]));
    }

    [Test]
    public void NonContiguousWriteTest()
    {
        using Sandbox sb = Sandbox.Create();

        using FrameFile frames = new(1, [1, 2, 2, 2], torch.ScalarType.Float32, sb.GetPath("frames.bin"));
        using torch.Tensor source = torch.arange(16, torch.ScalarType.Float32).reshape(1, 2, 2, 4);
        using torch.Tensor sliced = source.narrow(3, 0, 2);

        frames.Write(0, sliced);

        Assert.That(
            ReadValues(frames, 0),
            Is.EqualTo([0.0f, 1.0f, 4.0f, 5.0f, 8.0f, 9.0f, 12.0f, 13.0f]));
    }

    [Test]
    public void ConcurrentFilesTest()
    {
        using Sandbox sb = Sandbox.Create();

        List<FrameFile> files = [];
        List<float[]> written = [];

        for (int index = 0; index < 5; index++)
        {
            files.Add(new FrameFile(2, [1, 1, 2, 2], torch.ScalarType.Float32, sb.GetPath($"frames{index}.bin")));
            written.Add([index, index + 10.0f, index + 20.0f, index + 30.0f]);
        }

        for (int index = 0; index < files.Count; index++)
        {
            using torch.Tensor frame = MakeFrame(written[index]);

            files[index].Write(1, frame);
        }

        List<float[]> read = [];

        foreach (FrameFile file in files)
        {
            read.Add(ReadValues(file, 1));
        }

        foreach (FrameFile file in files)
        {
            file.Dispose();
        }

        Assert.That(read, Is.EqualTo(written));
        Assert.That(Directory.GetFiles(sb.RootPath), Is.Empty);
    }

    [Test]
    public void CleanupTest()
    {
        using Sandbox sb = Sandbox.Create();

        using (FrameFile frames = new(1, [1, 1, 2, 2], torch.ScalarType.Float32, sb.GetPath("frames.bin")))
        {
            using torch.Tensor frame = MakeFrame([1.0f, 2.0f, 3.0f, 4.0f]);

            frames.Write(0, frame);

            Assert.That(ReadValues(frames, 0), Is.EqualTo([1.0f, 2.0f, 3.0f, 4.0f]));
        }

        Assert.That(sb.GetTree(), Is.Empty);
    }
}
