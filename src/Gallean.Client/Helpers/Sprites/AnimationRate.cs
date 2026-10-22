namespace Gallean.Client;

internal readonly record struct AnimationRate(double FramesPerSecond)
{
    public static readonly AnimationRate Sprites = new(15);

    public int GetFrameIndex(TimeSpan time)
    {
        return (int)(time.TotalSeconds * FramesPerSecond);
    }

    public TimeSpan GetDuration(int frameCount)
    {
        return TimeSpan.FromSeconds(frameCount / FramesPerSecond);
    }

    public AnimationRate Scaled(double factor)
    {
        return new AnimationRate(FramesPerSecond * factor);
    }
}
