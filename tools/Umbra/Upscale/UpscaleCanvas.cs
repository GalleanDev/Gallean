using TorchSharp;

namespace Umbra;

internal static class UpscaleCanvas
{
    public static torch.Tensor Build(UpscaleClip clip, torch.Device device)
    {
        using IDisposable scope = torch.NewDisposeScope();

        torch.Tensor canvas = torch.zeros(
            [clip.Frames.Count, 4, clip.CanvasHeight, clip.CanvasWidth], torch.ScalarType.Float32, device);

        for (int position = 0; position < clip.Frames.Count; position++)
        {
            UpscaleClipFrame clipFrame = clip.Frames[position];
            UmbraSpriteMetadata metadata = clipFrame.Frame.Sprite.Metadata;

            canvas.select(0, position)
                .narrow(1, clipFrame.Top, metadata.PixelHeight)
                .narrow(2, clipFrame.Left, metadata.PixelWidth)
                .copy_(PixelTensor.ToPlanes(clipFrame.Frame.Sprite.Pixels, metadata.PixelWidth, metadata.PixelHeight, canvas.device));
        }

        return PixelOperations.SpreadColor(canvas).MoveToOuterDisposeScope();
    }
}
