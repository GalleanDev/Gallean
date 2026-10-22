namespace Gallean.Client;

internal sealed record AnimatedTextReveal(
    AnimatedTextSegmentType SegmentType,
    TimeSpan Stagger,
    TimeSpan Jitter,
    TimeSpan Duration,
    double Scatter,
    double Rise,
    double Scale,
    double Spin,
    double Curl,
    double Blur,
    double Smoke,
    double SmokeLift,
    double SmokeSwirl,
    double GlowBlur)
{
    public static AnimatedTextReveal SmokeGlyphs { get; } = new(
        AnimatedTextSegmentType.Glyph,
        Stagger: TimeSpan.FromMilliseconds(75),
        Jitter: TimeSpan.FromMilliseconds(400),
        Duration: TimeSpan.FromMilliseconds(1750),
        Scatter: 14.0,
        Rise: 6.0,
        Scale: 1.15,
        Spin: 8.0,
        Curl: 0.8,
        Blur: 8.0,
        Smoke: 2.2,
        SmokeLift: 60.0,
        SmokeSwirl: 220.0,
        GlowBlur: 8.0);

    public static AnimatedTextReveal SmokeWords { get; } = new(
        AnimatedTextSegmentType.Word,
        Stagger: TimeSpan.FromMilliseconds(20),
        Jitter: TimeSpan.Zero,
        Duration: TimeSpan.FromMilliseconds(1150),
        Scatter: 0.0,
        Rise: 6.0,
        Scale: 1.0,
        Spin: 0.0,
        Curl: 0.0,
        Blur: 3.0,
        Smoke: 0.9,
        SmokeLift: 80.0,
        SmokeSwirl: 130.0,
        GlowBlur: 4.0);

    public static AnimatedTextReveal FadeWords { get; } = new(
        AnimatedTextSegmentType.Word,
        Stagger: TimeSpan.FromMilliseconds(35),
        Jitter: TimeSpan.Zero,
        Duration: TimeSpan.FromMilliseconds(450),
        Scatter: 0.0,
        Rise: 5.0,
        Scale: 1.0,
        Spin: 0.0,
        Curl: 0.0,
        Blur: 3.0,
        Smoke: 0.0,
        SmokeLift: 0.0,
        SmokeSwirl: 0.0,
        GlowBlur: 5.0);
}
