using System.Reactive.Subjects;
using Gallean.Common;

namespace Gallean.Engine;

internal abstract class AbstractEngine<TState, TError, TEvent> where TError : struct
{
    protected readonly IEngineContext Context;
    private readonly IReadOnlyList<IEventRule<TState, TEvent>> eventRules;
    private readonly Subject<EngineStep<TState, TEvent>> engineEvents;
    private readonly IEngineContext dryRunContext;
    private readonly IReadOnlyList<IEventRule<TState, TEvent>> dryRunEventRules;

    protected AbstractEngine(
        IEngineContext context,
        TState initialState,
        Func<IEngineContext, IReadOnlyList<IEventRule<TState, TEvent>>> rulesFactory)
    {
        Context = context;
        State = initialState;

        eventRules = rulesFactory(context);
        engineEvents = new Subject<EngineStep<TState, TEvent>>();
        dryRunContext = new EngineContext(new FixedRandomProvider(), context.Definitions, context.UnitStats, context.Scenario, context.Globals);
        dryRunEventRules = rulesFactory(dryRunContext);
    }

    public TState State { get; private set; }
    public IObservable<EngineStep<TState, TEvent>> Events => engineEvents;

    public virtual ICommandResult<TError, TEvent, TResult> DispatchCommand<TResult>(
        ICommand<TState, TError, TEvent, TResult> command,
        bool dryRun = false)
    {
        IEngineContext context = dryRun ? dryRunContext : Context;
        IReadOnlyList<IEventRule<TState, TEvent>> rules = dryRun ? dryRunEventRules : eventRules;

        ICommandResult<TError, TEvent, TResult> commandResult = command.Execute(State, context);
        if (commandResult.Failed)
        {
            return commandResult;
        }

        List<TEvent> events = [];
        TState state = State;
        LinkedList<TEvent> queue = new(commandResult.Events);

        while (queue.TryRemoveFirst(out TEvent? ev))
        {
            TState previewState = ApplyEvent(ev, state);

            bool consumeEvent = false;
            List<TEvent> generatedEvents = [];
            foreach (IEventRule<TState, TEvent> rule in rules)
            {
                EventRuleResult<TEvent> ruleResult = rule.Apply(ev, previewState);
                generatedEvents.AddRange(ruleResult.Events);

                if (ruleResult.ConsumeEvent)
                {
                    consumeEvent = true;
                    break;
                }
            }

            if (!consumeEvent && ev is IScopeBeginEvent<TEvent> scopeBeginEvent)
            {
                generatedEvents.Add(scopeBeginEvent.CreateScopeEndEvent());
            }

            queue.AddRangeFirst(generatedEvents);

            if (!consumeEvent)
            {
                state = previewState;
                events.Add(ev);
            }
        }

        if (!dryRun)
        {
            foreach (TEvent ev in events)
            {
                TState before = State;

                State = ApplyEvent(ev, State);
                engineEvents.OnNext(new EngineStep<TState, TEvent>(before, State, ev));
            }
        }

        return ICommandResult<TError, TEvent, TResult>.Success(commandResult.Data, events);
    }

    protected abstract TState ApplyEvent(TEvent ev, TState state);
}
