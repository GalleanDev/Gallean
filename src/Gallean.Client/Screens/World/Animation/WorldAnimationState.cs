namespace Gallean.Client;

internal closed partial record WorldAnimationState
{
    public virtual FocusInfo? GetFocusInfo(TimeSpan time)
    {
        return null;
    }
}
