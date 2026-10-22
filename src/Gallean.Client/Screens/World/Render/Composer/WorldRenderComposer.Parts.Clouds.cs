using Gallean.Common;
using static Gallean.Client.WorldRenderState;

namespace Gallean.Client;

internal sealed partial class WorldRenderComposer
{
    private static class Clouds
    {
        public readonly record struct Input(int MapWidth, int MapHeight);

        private const int MinSize = 8;
        private const int SizePerCloud = 5;
        private const int MaxCount = 14;
        private const double MinPeriodSeconds = 55;
        private const double MaxPeriodSeconds = 95;
        private const int FrameOffsetCount = 64;

        public static IReadOnlyList<Cloud> Build(Input input)
        {
            int larger = Math.Max(input.MapWidth, input.MapHeight);
            int count = Math.Clamp((larger - MinSize) / SizePerCloud, 0, MaxCount);

            List<Cloud> clouds = new(count);

            for (int index = 0; index < count; index++)
            {
                int variant = index % AssetAnimationKey.Cloud.VariantCount;
                double heightFraction = 0.1 + (0.8 * GetFraction(index, 1));
                double period = MinPeriodSeconds + ((MaxPeriodSeconds - MinPeriodSeconds) * GetFraction(index, 2));
                double phase = GetFraction(index, 3);
                bool leftToRight = GetFraction(index, 4) < 0.5;

                clouds.Add(new Cloud(
                    new AssetAnimationKey.Cloud(variant),
                    FrameOffset: StableHash.Combine(index, 5).Mod(FrameOffsetCount),
                    heightFraction,
                    period,
                    phase,
                    leftToRight));
            }

            return clouds;
        }

        private static double GetFraction(int index, int salt)
        {
            return StableHash.Combine(index, salt) / (double)int.MaxValue;
        }
    }
}
