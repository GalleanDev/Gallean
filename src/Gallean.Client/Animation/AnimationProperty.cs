namespace Gallean.Client;

internal sealed record AnimationProperty(
    AnimationTarget Target,
    TimeSpan Start,
    TimeSpan Duration,
    double From,
    double To,
    AnimationEasing Easing)
{
    public double ValueAt(TimeSpan time)
    {
        double progress = time.ToAnimationProgress(Start, Duration);

        return double.Lerp(From, To, Easing.Ease(progress));
    }
}
