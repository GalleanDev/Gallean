namespace Gallean.Engine;

internal interface IEventRule<in TState, TEvent>
{
    EventRuleResult<TEvent> Apply(TEvent ev, TState state);
}
