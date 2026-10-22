using Avalonia;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Gallean.Common;
using SkiaSharp;

namespace Gallean.Client;

internal sealed class CursorAnimation : IDisposable
{
    private const float DragImageOpacity = 0.85f;

    private static readonly Vector BitmapDpi = new(96, 96);

    private readonly CleanupList cleanupList = new();
    private readonly List<Cursor> cursors = [];

    private CursorAnimation(PixelSize size, PixelPoint hotSpot)
    {
        Size = size;
        HotSpot = hotSpot;
    }

    public PixelSize Size { get; }
    public PixelPoint HotSpot { get; }
    public int FrameCount => cursors.Count;

    public Cursor this[int index] => cursors[index];

    public static CursorAnimation Create(Animation arrow, Animation? overlay, CursorDragImage? dragImage, double scaling)
    {
        AssetAnimationMetadata metadata = arrow.Metadata;
        int left = Math.Min(metadata.OffsetX, 0);
        int top = Math.Min(metadata.OffsetY, 0);
        int right = Math.Max(metadata.OffsetX + metadata.Width, 1);
        int bottom = Math.Max(metadata.OffsetY + metadata.Height, 1);
        int frameCount = metadata.FrameCount;

        if (overlay is not null)
        {
            AssetAnimationMetadata overlayMetadata = overlay.Metadata;

            left = Math.Min(left, overlayMetadata.OffsetX);
            top = Math.Min(top, overlayMetadata.OffsetY);
            right = Math.Max(right, overlayMetadata.OffsetX + overlayMetadata.Width);
            bottom = Math.Max(bottom, overlayMetadata.OffsetY + overlayMetadata.Height);
            frameCount = overlayMetadata.FrameCount;
        }

        if (dragImage is not null)
        {
            SKRect dragTarget = GetDragTarget(dragImage);

            left = Math.Min(left, (int)Math.Floor(dragTarget.Left));
            top = Math.Min(top, (int)Math.Floor(dragTarget.Top));
            right = Math.Max(right, (int)Math.Ceiling(dragTarget.Right));
            bottom = Math.Max(bottom, (int)Math.Ceiling(dragTarget.Bottom));
        }

        PixelSize size = new((int)Math.Ceiling((right - left) * scaling), (int)Math.Ceiling((bottom - top) * scaling));
        PixelPoint hotSpot = new((int)(-left * scaling), (int)(-top * scaling));

        CursorAnimation cursorAnimation = new(size, hotSpot);

        for (int index = 0; index < frameCount; index++)
        {
            cursorAnimation.Add(arrow[index % metadata.FrameCount], overlay?[index], dragImage, left, top, scaling);
        }

        return cursorAnimation;
    }

    public void Dispose()
    {
        cursors.Clear();
        cleanupList.Dispose();
    }

    private static SKRect GetDragTarget(CursorDragImage dragImage)
    {
        float width = (float)dragImage.Size.Width;
        float height = (float)dragImage.Size.Height;

        return SKRect.Create(-width / 2.0f, -height / 2.0f, width, height);
    }

    private void Add(Sprite arrow, Sprite? overlay, CursorDragImage? dragImage, int left, int top, double scaling)
    {
        using WriteableBitmap bitmap = new(Size, BitmapDpi, PixelFormat.Bgra8888, AlphaFormat.Premul);

        using (ILockedFramebuffer framebuffer = bitmap.Lock())
        {
            SKImageInfo info = new(Size.Width, Size.Height, SKColorType.Bgra8888, SKAlphaType.Premul);

            using SKSurface surface = SKSurface.Create(info, framebuffer.Address, framebuffer.RowBytes);
            surface.Canvas.Clear(SKColors.Transparent);
            surface.Canvas.Scale((float)scaling);

            if (dragImage is not null)
            {
                DrawDragImage(surface.Canvas, dragImage, left, top);
            }

            Draw(surface.Canvas, arrow, left, top);

            if (overlay is not null)
            {
                Draw(surface.Canvas, overlay, left, top);
            }
        }

        Cursor cursor = new(bitmap, HotSpot);

        cleanupList.Add(cursor);
        cursors.Add(cursor);
    }

    private static void DrawDragImage(SKCanvas canvas, CursorDragImage dragImage, int left, int top)
    {
        SKRect target = GetDragTarget(dragImage);

        target.Offset(-left, -top);

        using SKPaint paint = new() { Color = SKColors.White.WithAlpha((byte)(DragImageOpacity * 255)) };

        canvas.DrawImage(dragImage.Image, target, RenderSamplingOptions.Linear, paint);
    }

    private static void Draw(SKCanvas canvas, Sprite sprite, int left, int top)
    {
        SKRect target = SKRect.Create(
            sprite.Metadata.OffsetX - left,
            sprite.Metadata.OffsetY - top,
            sprite.Metadata.Width,
            sprite.Metadata.Height);

        canvas.DrawImage(sprite.Image, target, RenderSamplingOptions.Linear);
    }
}
