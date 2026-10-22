namespace Gallean.Client;

internal sealed partial class BattleAnimationStateBuilder
{
    private sealed class Step(TimeSpan start, TimeSpan duration)
    {
        public TimeSpan Start { get; } = start;
        public TimeSpan End { get; private set; } = start + duration;

        public void ExtendTo(TimeSpan end)
        {
            if (end > End)
            {
                End = end;
            }
        }
    }
}
