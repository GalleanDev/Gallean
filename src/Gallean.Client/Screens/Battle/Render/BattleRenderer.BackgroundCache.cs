using SkiaSharp;

namespace Gallean.Client;

internal sealed partial class BattleRenderer
{
    private sealed class BackgroundCache
    {
        private readonly record struct State(
            BattleBackgroundShaderProgram Program,
            SKMatrix Matrix,
            SKRectI Bounds,
            IntPtr ContextHandle);

        private State state;
        private SKData? uniforms;
        private SKImage? image;

        public void Draw(SKCanvas canvas, BattleBackgroundShaderProgram program)
        {
            using BattleBackgroundShaderProgram.Editor editor = program.Edit();

            State currentState = new(program, canvas.TotalMatrix, canvas.DeviceClipBounds, GetContextHandle(canvas));

            if (currentState.Bounds.IsEmpty)
            {
                DrawDirect(canvas, editor);

                return;
            }

            SKData currentUniforms = editor.ToUniformData();

            if (currentState == state && uniforms is not null && currentUniforms.AsSpan().SequenceEqual(uniforms.AsSpan()))
            {
                currentUniforms.Dispose();
                DrawCached(canvas, editor);
            }
            else
            {
                Reset(currentState, currentUniforms);
                DrawDirect(canvas, editor);
            }
        }

        private void Reset(State currentState, SKData currentUniforms)
        {
            image?.Dispose();
            uniforms?.Dispose();

            image = null;
            uniforms = currentUniforms;
            state = currentState;
        }

        private void DrawCached(SKCanvas canvas, BattleBackgroundShaderProgram.Editor editor)
        {
            image ??= RenderImage(canvas, editor);

            if (image is not null)
            {
                using SKPaint paint = new() { BlendMode = SKBlendMode.Src };

                canvas.Save();
                canvas.ResetMatrix();
                canvas.DrawImage(image, state.Bounds.Left, state.Bounds.Top, RenderSamplingOptions.Nearest, paint);
                canvas.Restore();
            }
            else
            {
                DrawDirect(canvas, editor);
            }
        }

        private static SKImage? RenderImage(SKCanvas target, BattleBackgroundShaderProgram.Editor editor)
        {
            SKRectI bounds = target.DeviceClipBounds;
            SKImageInfo info = new(bounds.Width, bounds.Height, SKImageInfo.PlatformColorType, SKAlphaType.Premul);
            GRRecordingContext? context = target.Context;

            using SKSurface? surface = context is not null ? SKSurface.Create(context, false, info) : SKSurface.Create(info);

            if (surface is not null)
            {
                surface.Canvas.SetMatrix(SKMatrix.Concat(SKMatrix.CreateTranslation(-bounds.Left, -bounds.Top), target.TotalMatrix));
                DrawDirect(surface.Canvas, editor);

                return surface.Snapshot();
            }
            else
            {
                return null;
            }
        }

        private static void DrawDirect(SKCanvas canvas, BattleBackgroundShaderProgram.Editor editor)
        {
            using SKShader shader = editor.Build();
            using SKPaint paint = new() { Shader = shader };

            canvas.DrawRect(canvas.LocalClipBounds, paint);
        }

        private static IntPtr GetContextHandle(SKCanvas canvas)
        {
            GRRecordingContext? context = canvas.Context;

            if (context is not null)
            {
                return context.Handle;
            }
            else
            {
                return IntPtr.Zero;
            }
        }
    }
}
