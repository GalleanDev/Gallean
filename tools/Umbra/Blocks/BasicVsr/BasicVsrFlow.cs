using TorchSharp;

namespace Umbra;

internal static class BasicVsrFlow
{
    public static torch.Tensor Warp(torch.Tensor planes, torch.Tensor flow, torch.GridSamplePaddingMode padding)
    {
        long height = planes.shape[2];
        long width = planes.shape[3];

        torch.Tensor columns = torch.arange(width, planes.dtype).reshape(1, width).expand(height, width);
        torch.Tensor rows = torch.arange(height, planes.dtype).reshape(height, 1).expand(height, width);
        torch.Tensor grid = torch.stack([columns, rows], dim: 2) + flow;
        torch.Tensor sampleX = grid.select(3, 0).mul(2.0).div(Math.Max(width - 1, 1)).sub(1.0);
        torch.Tensor sampleY = grid.select(3, 1).mul(2.0).div(Math.Max(height - 1, 1)).sub(1.0);

        return torch.nn.functional.grid_sample(
            planes,
            torch.stack([sampleX, sampleY], dim: 3),
            torch.GridSampleMode.Bilinear,
            padding,
            align_corners: true);
    }
}
