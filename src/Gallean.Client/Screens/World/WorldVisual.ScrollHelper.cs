using Avalonia;

namespace Gallean.Client;

internal sealed partial class WorldVisual
{
    internal sealed class ScrollHelper
    {
        private const double Margin = 24;

        private Point? pointerPosition;
        private bool enabled;

        public void HandlePointerMoved(Point position)
        {
            if (pointerPosition is { } previous && previous != position)
            {
                enabled = true;
            }

            pointerPosition = position;
        }

        public void HandlePointerExited()
        {
            pointerPosition = null;
        }

        public void HandleInputReleased()
        {
            pointerPosition = null;
            enabled = false;
        }

        public Vector GetDirection(Size viewport)
        {
            if (!enabled)
            {
                return default;
            }

            if (pointerPosition is null)
            {
                return default;
            }

            double directionX = 0;
            double directionY = 0;

            Point position = pointerPosition.Value;

            if (position.X is >= 0 and < Margin)
            {
                directionX -= 1;
            }
            else if (position.X <= viewport.Width && position.X > viewport.Width - Margin)
            {
                directionX += 1;
            }

            if (position.Y is >= 0 and < Margin)
            {
                directionY -= 1;
            }
            else if (position.Y <= viewport.Height && position.Y > viewport.Height - Margin)
            {
                directionY += 1;
            }

            return new Vector(directionX, directionY);
        }
    }
}
