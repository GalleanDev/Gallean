namespace Gallean.Client;

internal sealed class AnimationPlayback<T> where T : class, IAnimationObject
{
    private bool completed;
    private TimeSpan start;

    public T? Current { get; private set; }

    public bool IsPlaying => Current is not null && !completed;

    public bool Register(T? animation, TimeSpan now)
    {
        if (ReferenceEquals(animation, Current))
        {
            return false;
        }

        Current = animation;
        start = now;
        completed = animation is null;

        return true;
    }

    public TimeSpan GetElapsed(TimeSpan now)
    {
        return Current is not null ? now - start : TimeSpan.Zero;
    }

    public bool TryComplete(TimeSpan now)
    {
        if (!completed && Current is not null && now - start >= Current.Duration)
        {
            completed = true;
            return true;
        }
        else
        {
            return false;
        }
    }
}
