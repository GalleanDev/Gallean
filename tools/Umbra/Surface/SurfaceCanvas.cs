using TorchSharp;

namespace Umbra;

internal sealed partial class SurfaceCanvas
{
    private readonly IReadOnlyList<Placement> placements;

    private SurfaceCanvas(int pixelWidth, int pixelHeight, IReadOnlyList<Placement> placements)
    {
        this.placements = placements;

        PixelWidth = pixelWidth;
        PixelHeight = pixelHeight;
    }

    public int PixelWidth { get; }
    public int PixelHeight { get; }

    public static SurfaceCanvas Build(IReadOnlyList<UmbraFrame> frames)
    {
        int scale = ResolveScale(frames);
        int left = int.MaxValue;
        int top = int.MaxValue;
        int right = int.MinValue;
        int bottom = int.MinValue;

        foreach (UmbraFrame frame in frames)
        {
            UmbraSpriteMetadata metadata = frame.Sprite.Metadata;

            left = Math.Min(left, metadata.OffsetX);
            top = Math.Min(top, metadata.OffsetY);
            right = Math.Max(right, metadata.OffsetX + metadata.Width);
            bottom = Math.Max(bottom, metadata.OffsetY + metadata.Height);
        }

        List<Placement> placements = [];

        foreach (UmbraFrame frame in frames)
        {
            UmbraSpriteMetadata metadata = frame.Sprite.Metadata;

            placements.Add(new Placement(
                (metadata.OffsetX - left) * scale,
                (metadata.OffsetY - top) * scale,
                metadata.PixelWidth,
                metadata.PixelHeight));
        }

        return new SurfaceCanvas((right - left) * scale, (bottom - top) * scale, placements);
    }

    public torch.Tensor Compose(int index, UmbraSprite sprite, double background)
    {
        using IDisposable scope = torch.NewDisposeScope();

        Placement placement = placements[index];
        UmbraSpriteMetadata metadata = sprite.Metadata;

        torch.Tensor canvas = torch.full([3, PixelHeight, PixelWidth], background, torch.ScalarType.Float32);
        torch.Tensor planes = PixelTensor.ToPlanes(sprite.Pixels, metadata.PixelWidth, metadata.PixelHeight, canvas.device);
        torch.Tensor alpha = planes.narrow(0, 3, 1);
        torch.Tensor backdrop = torch.full([1, 1, 1], background, alpha.dtype).mul(alpha.neg().add(1.0));
        torch.Tensor color = planes.narrow(0, 0, 3).mul(alpha).add(backdrop);

        canvas
            .narrow(1, placement.Top, placement.PixelHeight)
            .narrow(2, placement.Left, placement.PixelWidth)
            .copy_(color);

        return canvas.MoveToOuterDisposeScope();
    }

    public torch.Tensor GetCoverage(int index, UmbraSprite sprite)
    {
        using IDisposable scope = torch.NewDisposeScope();

        Placement placement = placements[index];
        UmbraSpriteMetadata metadata = sprite.Metadata;

        torch.Tensor canvas = torch.zeros([1, PixelHeight, PixelWidth], torch.ScalarType.Float32);
        torch.Tensor planes = PixelTensor.ToPlanes(sprite.Pixels, metadata.PixelWidth, metadata.PixelHeight, canvas.device);

        canvas
            .narrow(1, placement.Top, placement.PixelHeight)
            .narrow(2, placement.Left, placement.PixelWidth)
            .copy_(planes.narrow(0, 3, 1));

        return canvas.MoveToOuterDisposeScope();
    }

    public torch.Tensor Extract(int index, torch.Tensor planes)
    {
        using IDisposable scope = torch.NewDisposeScope();

        Placement placement = placements[index];

        torch.Tensor spritePlanes = planes
            .narrow(1, placement.Top, placement.PixelHeight)
            .narrow(2, placement.Left, placement.PixelWidth)
            .contiguous();

        return spritePlanes.MoveToOuterDisposeScope();
    }

    private static int ResolveScale(IReadOnlyList<UmbraFrame> frames)
    {
        int scale = frames[0].Sprite.Metadata.PixelScale;

        foreach (UmbraFrame frame in frames)
        {
            if (frame.Sprite.Metadata.PixelScale != scale)
            {
                throw new InvalidOperationException("The sequence has frames with different pixel scales.");
            }
        }

        return scale;
    }
}
