namespace Gallean.Engine;

public interface IScopeBeginEvent<out TEndEvent>
{
    TEndEvent CreateScopeEndEvent();
}
