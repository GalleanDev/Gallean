using TorchSharp;

namespace Umbra;

internal static class PixelTensor
{
    public static torch.Tensor ToPlanes(ReadOnlyMemory<byte> pixels, int width, int height, torch.Device device)
    {
        using IDisposable scope = torch.NewDisposeScope();

        int channels = pixels.Length / (width * height);

        torch.Tensor rows = torch.tensor(
            pixels.ToArray(),
            [height, width, channels],
            torch.ScalarType.Byte,
            device);

        torch.Tensor planes = rows.permute(2, 0, 1).to(torch.ScalarType.Float32).div(255.0);

        return planes.MoveToOuterDisposeScope();
    }

    public static byte[] ToPixels(torch.Tensor planes)
    {
        using IDisposable scope = torch.NewDisposeScope();

        torch.Tensor levels = planes.mul(255.0).round().clamp(0.0, 255.0).to(torch.ScalarType.Byte);
        torch.Tensor rows = levels.permute(1, 2, 0).contiguous().cpu();

        return rows.data<byte>().ToArray();
    }
}
