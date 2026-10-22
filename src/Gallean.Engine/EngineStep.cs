namespace Gallean.Engine;

public readonly record struct EngineStep<TState, TEvent>(TState OldState, TState NewState, TEvent Event);
