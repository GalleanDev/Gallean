using Gallean.Engine;

namespace Gallean.Client;

internal sealed class EventLogBattleFormatter(IGameDefinitionProvider definitions, IGameTextProvider texts) :
    IBattleEventVisitor<BattleState, IReadOnlyList<EventLogEntry>>
{
    public IReadOnlyList<EventLogEntry> Format(BattleEvent ev, BattleState state)
    {
        return ev.Accept(this, state);
    }

    IReadOnlyList<EventLogEntry> IBattleEventVisitor<BattleState, IReadOnlyList<EventLogEntry>>.Visit(BattleUnitAttackEvent ev, BattleState context)
    {
        return AddEvent($"{Unit(ev.AttackerId, context)} attacked.");
    }

    IReadOnlyList<EventLogEntry> IBattleEventVisitor<BattleState, IReadOnlyList<EventLogEntry>>.Visit(BattleAutoResolveStepEvent ev, BattleState context)
    {
        return AddEvent($"Auto-resolve advanced from round {Value(ev.StartRoundNumber)}.");
    }

    IReadOnlyList<EventLogEntry> IBattleEventVisitor<BattleState, IReadOnlyList<EventLogEntry>>.Visit(BattleEndedEvent ev, BattleState context)
    {
        return AddEvent($"Battle ended. {Side(ev.WinningSide)} won.");
    }

    IReadOnlyList<EventLogEntry> IBattleEventVisitor<BattleState, IReadOnlyList<EventLogEntry>>.Visit(BattleUnitExperienceGainedEvent ev, BattleState context)
    {
        if (ev.ExperienceGained > 0)
        {
            return AddEvent($"{Unit(ev.UnitId, context)} has gained {Value(ev.ExperienceGained)} XP this battle.");
        }
        else
        {
            return [];
        }
    }

    IReadOnlyList<EventLogEntry> IBattleEventVisitor<BattleState, IReadOnlyList<EventLogEntry>>.Visit(BattleUnitUpgradedEvent ev, BattleState context)
    {
        if (ev.ToDefinitionId == ev.FromDefinitionId)
        {
            return AddEvent($"{UnitType(ev.FromDefinitionId)} reached level {Value(ev.Level)}.");
        }
        else
        {
            return AddEvent($"{UnitType(ev.FromDefinitionId)} advanced to {UnitType(ev.ToDefinitionId)}.");
        }
    }

    IReadOnlyList<EventLogEntry> IBattleEventVisitor<BattleState, IReadOnlyList<EventLogEntry>>.Visit(BattleRoundStartedEvent ev, BattleState context)
    {
        return ev.Round.Type switch
        {
            BattleRoundType.Opening => AddEvent($"Battle opening round started."),
            BattleRoundType.Normal => AddEvent($"Battle round {Value(ev.Round.Number)} started."),
            BattleRoundType.Recovery => AddEvent($"Battle recovery round started.")
        };
    }

    IReadOnlyList<EventLogEntry> IBattleEventVisitor<BattleState, IReadOnlyList<EventLogEntry>>.Visit(BattleRoundEndedEvent ev, BattleState context)
    {
        return [];
    }

    IReadOnlyList<EventLogEntry> IBattleEventVisitor<BattleState, IReadOnlyList<EventLogEntry>>.Visit(BattleUnitAttackHitEvent ev, BattleState context)
    {
        if (ev.Damage > 0)
        {
            return AddEvent($"{Unit(ev.AttackerId, context)} hit {Unit(ev.TargetId, context)} for {Damage(ev.Damage)} damage.");
        }
        else
        {
            return [];
        }
    }

    IReadOnlyList<EventLogEntry> IBattleEventVisitor<BattleState, IReadOnlyList<EventLogEntry>>.Visit(BattleUnitAttackStepEvent ev, BattleState context)
    {
        return [];
    }

    IReadOnlyList<EventLogEntry> IBattleEventVisitor<BattleState, IReadOnlyList<EventLogEntry>>.Visit(BattleUnitAttackMissedEvent ev, BattleState context)
    {
        return AddEvent($"{Unit(ev.AttackerId, context)} missed {Unit(ev.TargetId, context)}.");
    }

    IReadOnlyList<EventLogEntry> IBattleEventVisitor<BattleState, IReadOnlyList<EventLogEntry>>.Visit(BattleUnitAttackBlockedEvent ev, BattleState context)
    {
        return AddEvent($"{Unit(ev.AttackerId, context)} was blocked by {Unit(ev.TargetId, context)}'s {Ward(ev.Ward)} ward.");
    }

    IReadOnlyList<EventLogEntry> IBattleEventVisitor<BattleState, IReadOnlyList<EventLogEntry>>.Visit(BattleUnitAttackedEvent ev, BattleState context)
    {
        return [];
    }

    IReadOnlyList<EventLogEntry> IBattleEventVisitor<BattleState, IReadOnlyList<EventLogEntry>>.Visit(BattleUnitDefendedEvent ev, BattleState context)
    {
        return AddEvent($"{Unit(ev.UnitId, context)} defended.");
    }

    IReadOnlyList<EventLogEntry> IBattleEventVisitor<BattleState, IReadOnlyList<EventLogEntry>>.Visit(BattleUnitDamagedByEffectEvent ev, BattleState context)
    {
        return AddEvent($"{Unit(ev.UnitId, context)} took {Damage(ev.Damage)} effect damage.");
    }

    IReadOnlyList<EventLogEntry> IBattleEventVisitor<BattleState, IReadOnlyList<EventLogEntry>>.Visit(BattleUnitDiedEvent ev, BattleState context)
    {
        return AddEvent($"{Unit(ev.UnitId, context)} died.");
    }

    IReadOnlyList<EventLogEntry> IBattleEventVisitor<BattleState, IReadOnlyList<EventLogEntry>>.Visit(BattleUnitHealedEvent ev, BattleState context)
    {
        return AddEvent($"{Unit(ev.UnitId, context)} healed to {Healing(ev.HitpointsAfter)} HP.");
    }

    IReadOnlyList<EventLogEntry> IBattleEventVisitor<BattleState, IReadOnlyList<EventLogEntry>>.Visit(BattleUnitRevivedEvent ev, BattleState context)
    {
        return AddEvent($"{Unit(ev.UnitId, context)} was revived with {Healing(ev.HitpointsAfter)} HP.");
    }

    IReadOnlyList<EventLogEntry> IBattleEventVisitor<BattleState, IReadOnlyList<EventLogEntry>>.Visit(BattleUnitCuredEvent ev, BattleState context)
    {
        return AddEvent($"{Unit(ev.UnitId, context)} was cured.");
    }

    IReadOnlyList<EventLogEntry> IBattleEventVisitor<BattleState, IReadOnlyList<EventLogEntry>>.Visit(BattleUnitAttackGrantedEvent ev, BattleState context)
    {
        return AddEvent($"{Unit(ev.UnitId, context)} was granted an extra attack.");
    }

    IReadOnlyList<EventLogEntry> IBattleEventVisitor<BattleState, IReadOnlyList<EventLogEntry>>.Visit(BattleItemUsedEvent ev, BattleState context)
    {
        return AddEvent($"{Unit(ev.UnitId, context)} used {Item(ev.ItemId)}.");
    }

    IReadOnlyList<EventLogEntry> IBattleEventVisitor<BattleState, IReadOnlyList<EventLogEntry>>.Visit(BattleUnitSummonedEvent ev, BattleState context)
    {
        return AddEvent($"{Unit(ev.SummonerId, context)} summoned {UnitType(ev.DefinitionId)}.");
    }

    IReadOnlyList<EventLogEntry> IBattleEventVisitor<BattleState, IReadOnlyList<EventLogEntry>>.Visit(BattleUnitUnsummonedEvent ev, BattleState context)
    {
        return AddEvent($"{Unit(ev.UnitId, context)} vanished.");
    }

    IReadOnlyList<EventLogEntry> IBattleEventVisitor<BattleState, IReadOnlyList<EventLogEntry>>.Visit(BattleUnitTransformAppliedEvent ev, BattleState context)
    {
        return ev.Type switch
        {
            BattleTransformType.Transform => AddEvent($"{Unit(ev.TargetId, context)} transformed into {UnitType(ev.DefinitionId)}."),
            BattleTransformType.TransformSelf => AddEvent($"{Unit(ev.TargetId, context)} transformed into {UnitType(ev.DefinitionId)}."),
            BattleTransformType.Doppelganger => AddEvent($"{Unit(ev.TargetId, context)} transformed into {UnitType(ev.DefinitionId)}."),
            BattleTransformType.LevelDrain => AddEvent($"{Unit(ev.TargetId, context)} was reduced to {UnitType(ev.DefinitionId)} of level {Value(ev.Level)}.")
        };
    }

    IReadOnlyList<EventLogEntry> IBattleEventVisitor<BattleState, IReadOnlyList<EventLogEntry>>.Visit(BattleUnitTransformRestoredEvent ev, BattleState context)
    {
        return ev.Type switch
        {
            BattleTransformType.Transform => AddEvent($"{Unit(ev.UnitId, context)} returned to {UnitType(ev.DefinitionId)}."),
            BattleTransformType.TransformSelf => AddEvent($"{Unit(ev.UnitId, context)} returned to {UnitType(ev.DefinitionId)}."),
            BattleTransformType.Doppelganger => AddEvent($"{Unit(ev.UnitId, context)} returned to {UnitType(ev.DefinitionId)}."),
            BattleTransformType.LevelDrain => AddEvent($"{Unit(ev.UnitId, context)} regained the level of {UnitType(ev.DefinitionId)}.")
        };
    }

    IReadOnlyList<EventLogEntry> IBattleEventVisitor<BattleState, IReadOnlyList<EventLogEntry>>.Visit(BattleUnitEffectAppliedEvent ev, BattleState context)
    {
        return AddEvent($"{Unit(ev.TargetId, context)} received status {StatusEffect(ev.Effect)}.");
    }

    IReadOnlyList<EventLogEntry> IBattleEventVisitor<BattleState, IReadOnlyList<EventLogEntry>>.Visit(BattleUnitModifierAppliedEvent ev, BattleState context)
    {
        return AddEvent($"{Unit(ev.UnitId, context)} received modifier {Modifier(ev.Modifier.DefinitionId)}.");
    }

    IReadOnlyList<EventLogEntry> IBattleEventVisitor<BattleState, IReadOnlyList<EventLogEntry>>.Visit(BattleUnitEffectDurationDecreasedEvent ev, BattleState context)
    {
        return [];
    }

    IReadOnlyList<EventLogEntry> IBattleEventVisitor<BattleState, IReadOnlyList<EventLogEntry>>.Visit(BattleUnitEffectExpiredEvent ev, BattleState context)
    {
        return AddEvent($"{Unit(ev.UnitId, context)} lost status {StatusEffect(ev.Effect)}.");
    }

    IReadOnlyList<EventLogEntry> IBattleEventVisitor<BattleState, IReadOnlyList<EventLogEntry>>.Visit(BattleUnitTurnStartedEvent ev, BattleState context)
    {
        return [];
    }

    IReadOnlyList<EventLogEntry> IBattleEventVisitor<BattleState, IReadOnlyList<EventLogEntry>>.Visit(BattleUnitTurnSkippedEvent ev, BattleState context)
    {
        return AddEvent($"{Unit(ev.UnitId, context)} skipped its turn.");
    }

    IReadOnlyList<EventLogEntry> IBattleEventVisitor<BattleState, IReadOnlyList<EventLogEntry>>.Visit(BattleUnitTurnEndingEvent ev, BattleState context)
    {
        return [];
    }

    IReadOnlyList<EventLogEntry> IBattleEventVisitor<BattleState, IReadOnlyList<EventLogEntry>>.Visit(BattleUnitTurnEndedEvent ev, BattleState context)
    {
        return [];
    }

    IReadOnlyList<EventLogEntry> IBattleEventVisitor<BattleState, IReadOnlyList<EventLogEntry>>.Visit(BattleUnitWaitedEvent ev, BattleState context)
    {
        return AddEvent($"{Unit(ev.UnitId, context)} waited.");
    }

    IReadOnlyList<EventLogEntry> IBattleEventVisitor<BattleState, IReadOnlyList<EventLogEntry>>.Visit(BattleUnitWardConsumedEvent ev, BattleState context)
    {
        return AddEvent($"{Unit(ev.UnitId, context)} consumed {Ward(ev.Ward)} ward.");
    }

    IReadOnlyList<EventLogEntry> IBattleEventVisitor<BattleState, IReadOnlyList<EventLogEntry>>.Visit(BattleUnitWardGrantedEvent ev, BattleState context)
    {
        return AddEvent($"{Unit(ev.UnitId, context)} gained {Ward(ev.Modifier.Ward)} ward.");
    }

    IReadOnlyList<EventLogEntry> IBattleEventVisitor<BattleState, IReadOnlyList<EventLogEntry>>.Visit(BattleUnitWardExpiredEvent ev, BattleState context)
    {
        return AddEvent($"{Unit(ev.UnitId, context)} lost {Ward(ev.Modifier.Ward)} ward.");
    }

    IReadOnlyList<EventLogEntry> IBattleEventVisitor<BattleState, IReadOnlyList<EventLogEntry>>.Visit(BattleUnitRetreatingEvent ev, BattleState context)
    {
        return AddEvent($"{Unit(ev.UnitId, context)} started retreating.");
    }

    IReadOnlyList<EventLogEntry> IBattleEventVisitor<BattleState, IReadOnlyList<EventLogEntry>>.Visit(BattleUnitRetreatedEvent ev, BattleState context)
    {
        return AddEvent($"{Unit(ev.UnitId, context)} retreated.");
    }

    private static IReadOnlyList<EventLogEntry> AddEvent(EventLogInterpolatedStringHandler text)
    {
        return [new EventLogEntry(EventLogEntryType.Battle, text.ToImmutable(), null)];
    }

    private EventLogSpan Unit(BattleUnitId id, BattleState state)
    {
        string name = UnitNameResolver.Resolve(state.GetUnit(id), definitions, texts);

        return new EventLogSpan(name, EventLogSpanType.Actor);
    }

    private EventLogSpan UnitType(UnitDefinitionId id)
    {
        UnitDefinition unit = definitions.GetUnit(id);

        return new EventLogSpan(Text(unit.NameTextId), EventLogSpanType.Actor);
    }

    private EventLogSpan Item(ItemDefinitionId id)
    {
        ItemDefinition item = definitions.GetItem(id);

        return new EventLogSpan(Text(item.NameTextId), EventLogSpanType.Object);
    }

    private static EventLogSpan Ward(UnitWard ward)
    {
        return new EventLogSpan(UnitWardFormatter.FormatName(ward), EventLogSpanType.Object);
    }

    private EventLogSpan Modifier(UnitModifierDefinitionId id)
    {
        UnitModifierDefinition modifier = definitions.GetUnitModifier(id);

        return new EventLogSpan(Text(modifier.DescriptionTextId), EventLogSpanType.Object);
    }

    private static EventLogSpan StatusEffect(BattleAbstractStatusEffect effect)
    {
        return new EventLogSpan(BattleStatusEffectFormatter.FormatName(effect), EventLogSpanType.Object);
    }

    private static EventLogSpan Value(int value)
    {
        return new EventLogSpan(value.ToString(), EventLogSpanType.Value);
    }

    private static EventLogSpan Damage(int damage)
    {
        return new EventLogSpan(damage.ToString(), EventLogSpanType.Damage);
    }

    private static EventLogSpan Healing(int hitpoints)
    {
        return new EventLogSpan(hitpoints.ToString(), EventLogSpanType.Healing);
    }

    private static EventLogSpan Side(BattleSide side)
    {
        return side switch
        {
            BattleSide.Attacker => new EventLogSpan("Attacker", EventLogSpanType.Actor),
            BattleSide.Defender => new EventLogSpan("Defender", EventLogSpanType.Actor)
        };
    }

    private string Text(GlobalTextId id)
    {
        return texts.GetGlobalText(id).Text;
    }
}
