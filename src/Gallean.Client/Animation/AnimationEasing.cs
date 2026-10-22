namespace Gallean.Client;

internal abstract record AnimationEasing
{
    public sealed record LinearEasing : AnimationEasing
    {
        public override double Ease(double progress)
        {
            return Math.Clamp(progress, 0, 1);
        }
    }

    public sealed record SmoothEasing : AnimationEasing
    {
        public override double Ease(double progress)
        {
            double clamped = Math.Clamp(progress, 0, 1);

            return clamped * clamped * (3 - 2 * clamped);
        }
    }

    public sealed record DecelerateEasing : AnimationEasing
    {
        public override double Ease(double progress)
        {
            double remaining = 1 - Math.Clamp(progress, 0, 1);

            return 1 - (remaining * remaining * remaining);
        }
    }

    public static AnimationEasing Linear { get; } = new LinearEasing();
    public static AnimationEasing Smooth { get; } = new SmoothEasing();
    public static AnimationEasing Decelerate { get; } = new DecelerateEasing();

    public abstract double Ease(double progress);
}
