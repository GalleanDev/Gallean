namespace Gallean.Engine;

internal readonly record struct EventRuleResult<TEvent>(bool ConsumeEvent, IReadOnlyList<TEvent> Events)
{
    public static EventRuleResult<TEvent> Empty => new(false, []);

    public static EventRuleResult<TEvent> Append(params TEvent[] events)
    {
        return new EventRuleResult<TEvent>(false, events);
    }

    public static EventRuleResult<TEvent> Consume(params TEvent[] events)
    {
        return new EventRuleResult<TEvent>(true, events);
    }
}
