using Microsoft.Win32.SafeHandles;
using TorchSharp;

namespace Umbra;

internal sealed partial class FrameFile : IDisposable
{
    private readonly long[] dimensions;
    private readonly torch.ScalarType type;
    private readonly long frameBytes;
    private readonly SafeFileHandle handle;

    public FrameFile(int count, long[] dimensions, torch.ScalarType type, string path)
    {
        Count = count;
        this.dimensions = dimensions;
        this.type = type;

        long elements = 1;

        foreach (long dimension in dimensions)
        {
            elements *= dimension;
        }

        frameBytes = elements * type.ElementSize();
        handle = OpenScratchFile(path);
    }

    public int Count { get; }

    private static SafeFileHandle OpenScratchFile(string path)
    {
        if (OperatingSystem.IsLinux())
        {
            return Linux.memfd_create(Path.GetFileName(path), Linux.MFD_CLOEXEC);
        }
        else if (OperatingSystem.IsWindows())
        {
            return Windows.CreateFile(
                path,
                Windows.GENERIC_READ | Windows.GENERIC_WRITE,
                0,
                Windows.CREATE_NEW,
                Windows.FILE_ATTRIBUTE_TEMPORARY | Windows.FILE_FLAG_DELETE_ON_CLOSE);
        }
        else
        {
            return File.OpenHandle(
                path,
                FileMode.CreateNew,
                FileAccess.ReadWrite,
                FileShare.None,
                FileOptions.DeleteOnClose);
        }
    }

    public void Write(int index, torch.Tensor frame)
    {
        using IDisposable scope = torch.NewDisposeScope();

        torch.Tensor hostFrame = frame.contiguous().cpu();

        RandomAccess.Write(handle, hostFrame.bytes, index * frameBytes);
    }

    public torch.Tensor Read(int index)
    {
        using IDisposable scope = torch.NewDisposeScope();

        torch.Tensor frame = torch.empty(dimensions, type, torch.CPU);
        Span<byte> bytes = frame.bytes;
        int total = 0;

        while (total < bytes.Length)
        {
            int read = RandomAccess.Read(handle, bytes[total..], (index * frameBytes) + total);

            if (read <= 0)
            {
                throw new EndOfStreamException();
            }

            total += read;
        }

        return frame.to(torch.get_default_device()).MoveToOuterDisposeScope();
    }

    public void Dispose()
    {
        handle.Dispose();
    }
}
