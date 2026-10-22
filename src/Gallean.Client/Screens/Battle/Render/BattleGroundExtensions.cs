using Avalonia;

namespace Gallean.Client;

internal static class BattleGroundExtensions
{
    extension(BattleGround ground)
    {
        public double GetDistance(Point scene)
        {
            double column = (scene.X - ground.Bounds.X) / ground.Bounds.Width * ground.Columns;
            double row = (scene.Y - ground.Bounds.Y) / ground.Bounds.Height * ground.Rows;
            int x = Math.Clamp((int)column, 0, ground.Columns - 1);
            int y = Math.Clamp((int)row, 0, ground.Rows - 1);

            return ground.Depths[(y * ground.Columns) + x] * ground.DepthScale;
        }
    }
}
