namespace Gallean.Client;

internal abstract partial record WorldAnimationState
{
    public virtual FocusInfo? GetFocusInfo(TimeSpan time)
    {
        return null;
    }
}
