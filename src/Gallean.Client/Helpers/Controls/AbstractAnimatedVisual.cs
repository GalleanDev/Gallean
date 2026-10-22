using Avalonia;
using Avalonia.Controls;
using Avalonia.Rendering.Composition;
using Avalonia.Rendering.Composition.Transport;

namespace Gallean.Client;

internal abstract class AbstractAnimatedVisual : Control
{
    private bool frameRequested;
    private bool running;

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        running = true;
        StartFramesAsync().FileAndForget();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);

        running = false;
    }

    protected abstract void OnFrame(TimeSpan frameTime);

    protected void StopFrames()
    {
        running = false;
    }

    private async Task StartFramesAsync()
    {
        CompositionVisual? visual = ElementComposition.GetElementVisual(this);

        if (visual is not null)
        {
            CompositionBatch batch = visual.Compositor.RequestCompositionBatchCommitAsync();

            await batch.Rendered;
        }

        if (running)
        {
            RequestFrame();
        }
    }

    private void RequestFrame()
    {
        TopLevel? topLevel = TopLevel.GetTopLevel(this);

        if (!frameRequested && topLevel is not null)
        {
            frameRequested = true;
            topLevel.RequestAnimationFrame(OnAnimationFrame);
        }
    }

    private void OnAnimationFrame(TimeSpan frameTime)
    {
        frameRequested = false;

        if (running)
        {
            try
            {
                OnFrame(frameTime);
            }
            finally
            {
                if (running)
                {
                    RequestFrame();
                }
            }
        }
    }
}
