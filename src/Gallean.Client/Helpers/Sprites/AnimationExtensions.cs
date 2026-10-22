using Gallean.Common;

namespace Gallean.Client;

internal static class AnimationExtensions
{
    extension(Animation animation)
    {
        public Sprite FrameByIndex(int index, bool looping)
        {
            int frameCount = animation.Metadata.FrameCount;

            if (looping)
            {
                return animation[index.Mod(frameCount)];
            }
            else
            {
                return animation[Math.Clamp(index, 0, frameCount - 1)];
            }
        }

        public Sprite FrameByRatio(double ratio, bool looping)
        {
            return animation.FrameByIndex((int)(ratio * animation.Metadata.FrameCount), looping);
        }
    }

    public static double ToAnimationProgress(this TimeSpan time, TimeSpan start, TimeSpan duration)
    {
        if (duration > TimeSpan.Zero)
        {
            return Math.Clamp((time - start) / duration, 0, 1);
        }
        else if (time >= start)
        {
            return 1;
        }
        else
        {
            return 0;
        }
    }
}
