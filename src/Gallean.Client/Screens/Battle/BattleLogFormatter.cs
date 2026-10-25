using Gallean.Engine;

namespace Gallean.Client;

internal sealed class BattleLogFormatter(IGameDefinitionProvider definitions, IGameTextProvider texts) : IBattleEventVisitor<BattleState, BattleLogEntry?>
{
    public IReadOnlyList<BattleLogEntry> Format(IReadOnlyList<EngineStep<BattleState, BattleEvent>> steps)
    {
        List<BattleLogEntry> entries = [];
        bool hasEvents = false;

        foreach (EngineStep<BattleState, BattleEvent> step in steps)
        {
            BattleLogEntry? entry = Format(step.Event, step.OldState);

            if (entry is BattleLogEventEntry)
            {
                hasEvents = true;
            }

            if (entry is not null)
            {
                entries.Add(entry);
            }
        }

        if (hasEvents)
        {
            return entries;
        }
        else
        {
            return [];
        }
    }

    public BattleLogEntry? Format(BattleEvent ev, BattleState state)
    {
        return ev.Accept(this, state);
    }

    BattleLogEntry? IBattleEventVisitor<BattleState, BattleLogEntry?>.Visit(BattleUnitAttackEvent ev, BattleState context)
    {
        return null;
    }

    BattleLogEntry? IBattleEventVisitor<BattleState, BattleLogEntry?>.Visit(BattleAutoResolveStepEvent ev, BattleState context)
    {
        return null;
    }

    BattleLogEntry? IBattleEventVisitor<BattleState, BattleLogEntry?>.Visit(BattleEndedEvent ev, BattleState context)
    {
        return ev.WinningSide switch
        {
            BattleSide.Attacker => new BattleLogEventEntry(TrSpans("The attacker won.")),
            BattleSide.Defender => new BattleLogEventEntry(TrSpans("The defender won."))
        };
    }

    BattleLogEntry? IBattleEventVisitor<BattleState, BattleLogEntry?>.Visit(BattleUnitExperienceGainedEvent ev, BattleState context)
    {
        BattleUnit unit = context.GetUnit(ev.UnitId);
        int experience = ev.ExperienceGained - unit.ExperienceGained;

        if (experience > 0)
        {
            return new BattleLogEventEntry(TrSpans("{0:*} gained {1} experience.", Unit(ev.UnitId, context), experience));
        }
        else
        {
            return null;
        }
    }

    BattleLogEntry? IBattleEventVisitor<BattleState, BattleLogEntry?>.Visit(BattleUnitUpgradedEvent ev, BattleState context)
    {
        if (ev.ToDefinitionId == ev.FromDefinitionId)
        {
            return new BattleLogEventEntry(TrSpans("{0:*} reached level {1}.", UnitType(ev.FromDefinitionId), ev.Level));
        }
        else
        {
            return new BattleLogEventEntry(TrSpans("{0:*} upgraded to {1:*}.", UnitType(ev.FromDefinitionId), UnitType(ev.ToDefinitionId)));
        }
    }

    BattleLogEntry? IBattleEventVisitor<BattleState, BattleLogEntry?>.Visit(BattleRoundStartedEvent ev, BattleState context)
    {
        return ev.Round.Type switch
        {
            BattleRoundType.Opening => null,
            BattleRoundType.Normal => new BattleLogRoundEntry(Tr("Round {0}", ev.Round.Number)),
            BattleRoundType.Recovery => null
        };
    }

    BattleLogEntry? IBattleEventVisitor<BattleState, BattleLogEntry?>.Visit(BattleRoundEndedEvent ev, BattleState context)
    {
        return null;
    }

    BattleLogEntry? IBattleEventVisitor<BattleState, BattleLogEntry?>.Visit(BattleUnitAttackHitEvent ev, BattleState context)
    {
        if (ev.Damage > 0)
        {
            return new BattleLogEventEntry(TrSpans("{0:*} hit {1:*} for {2} damage.", Unit(ev.AttackerId, context), Unit(ev.TargetId, context), ev.Damage));
        }
        else
        {
            return null;
        }
    }

    BattleLogEntry? IBattleEventVisitor<BattleState, BattleLogEntry?>.Visit(BattleUnitAttackStepEvent ev, BattleState context)
    {
        return null;
    }

    BattleLogEntry? IBattleEventVisitor<BattleState, BattleLogEntry?>.Visit(BattleUnitAttackMissedEvent ev, BattleState context)
    {
        return new BattleLogEventEntry(TrSpans("{0:*} missed {1:*}.", Unit(ev.AttackerId, context), Unit(ev.TargetId, context)));
    }

    BattleLogEntry? IBattleEventVisitor<BattleState, BattleLogEntry?>.Visit(BattleUnitAttackBlockedEvent ev, BattleState context)
    {
        return new BattleLogEventEntry(TrSpans("{0:*} was blocked by the {1:i} ward of {2:*}.", Unit(ev.AttackerId, context), UnitWardFormatter.FormatName(ev.Ward), Unit(ev.TargetId, context)));
    }

    BattleLogEntry? IBattleEventVisitor<BattleState, BattleLogEntry?>.Visit(BattleUnitAttackedEvent ev, BattleState context)
    {
        return null;
    }

    BattleLogEntry? IBattleEventVisitor<BattleState, BattleLogEntry?>.Visit(BattleUnitDefendedEvent ev, BattleState context)
    {
        return new BattleLogEventEntry(TrSpans("{0:*} defended.", Unit(ev.UnitId, context)));
    }

    BattleLogEntry? IBattleEventVisitor<BattleState, BattleLogEntry?>.Visit(BattleUnitDamagedByEffectEvent ev, BattleState context)
    {
        return new BattleLogEventEntry(TrSpans("{0:*} took {1} effect damage.", Unit(ev.UnitId, context), ev.Damage));
    }

    BattleLogEntry? IBattleEventVisitor<BattleState, BattleLogEntry?>.Visit(BattleUnitDiedEvent ev, BattleState context)
    {
        return new BattleLogEventEntry(TrSpans("{0:*} died.", Unit(ev.UnitId, context)));
    }

    BattleLogEntry? IBattleEventVisitor<BattleState, BattleLogEntry?>.Visit(BattleUnitHealedEvent ev, BattleState context)
    {
        return new BattleLogEventEntry(TrSpans("{0:*} healed to {1} hit points.", Unit(ev.UnitId, context), ev.HitpointsAfter));
    }

    BattleLogEntry? IBattleEventVisitor<BattleState, BattleLogEntry?>.Visit(BattleUnitRevivedEvent ev, BattleState context)
    {
        return new BattleLogEventEntry(TrSpans("{0:*} was revived with {1} hit points.", Unit(ev.UnitId, context), ev.HitpointsAfter));
    }

    BattleLogEntry? IBattleEventVisitor<BattleState, BattleLogEntry?>.Visit(BattleUnitCuredEvent ev, BattleState context)
    {
        return new BattleLogEventEntry(TrSpans("{0:*} was cured.", Unit(ev.UnitId, context)));
    }

    BattleLogEntry? IBattleEventVisitor<BattleState, BattleLogEntry?>.Visit(BattleUnitAttackGrantedEvent ev, BattleState context)
    {
        return new BattleLogEventEntry(TrSpans("{0:*} was granted an extra attack.", Unit(ev.UnitId, context)));
    }

    BattleLogEntry? IBattleEventVisitor<BattleState, BattleLogEntry?>.Visit(BattleItemUsedEvent ev, BattleState context)
    {
        return new BattleLogEventEntry(TrSpans("{0:*} used {1:i}.", Unit(ev.UnitId, context), Item(ev.ItemId)));
    }

    BattleLogEntry? IBattleEventVisitor<BattleState, BattleLogEntry?>.Visit(BattleUnitSummonedEvent ev, BattleState context)
    {
        return new BattleLogEventEntry(TrSpans("{0:*} summoned {1:*}.", Unit(ev.SummonerId, context), UnitType(ev.DefinitionId)));
    }

    BattleLogEntry? IBattleEventVisitor<BattleState, BattleLogEntry?>.Visit(BattleUnitUnsummonedEvent ev, BattleState context)
    {
        return new BattleLogEventEntry(TrSpans("{0:*} was unsummoned.", Unit(ev.UnitId, context)));
    }

    BattleLogEntry? IBattleEventVisitor<BattleState, BattleLogEntry?>.Visit(BattleUnitTransformAppliedEvent ev, BattleState context)
    {
        return ev.Type switch
        {
            BattleTransformType.Transform => new BattleLogEventEntry(TrSpans("{0:*} transformed into {1:*}.", Unit(ev.TargetId, context), UnitType(ev.DefinitionId))),
            BattleTransformType.TransformSelf => new BattleLogEventEntry(TrSpans("{0:*} transformed into {1:*}.", Unit(ev.TargetId, context), UnitType(ev.DefinitionId))),
            BattleTransformType.Doppelganger => new BattleLogEventEntry(TrSpans("{0:*} transformed into {1:*}.", Unit(ev.TargetId, context), UnitType(ev.DefinitionId))),
            BattleTransformType.LevelDrain => new BattleLogEventEntry(TrSpans("{0:*} was reduced to {1:*} of level {2}.", Unit(ev.TargetId, context), UnitType(ev.DefinitionId), ev.Level))
        };
    }

    BattleLogEntry? IBattleEventVisitor<BattleState, BattleLogEntry?>.Visit(BattleUnitTransformRestoredEvent ev, BattleState context)
    {
        return ev.Type switch
        {
            BattleTransformType.Transform => new BattleLogEventEntry(TrSpans("{0:*} returned to {1:*}.", Unit(ev.UnitId, context), UnitType(ev.DefinitionId))),
            BattleTransformType.TransformSelf => new BattleLogEventEntry(TrSpans("{0:*} returned to {1:*}.", Unit(ev.UnitId, context), UnitType(ev.DefinitionId))),
            BattleTransformType.Doppelganger => new BattleLogEventEntry(TrSpans("{0:*} returned to {1:*}.", Unit(ev.UnitId, context), UnitType(ev.DefinitionId))),
            BattleTransformType.LevelDrain => new BattleLogEventEntry(TrSpans("{0:*} regained the level of {1:*}.", Unit(ev.UnitId, context), UnitType(ev.DefinitionId)))
        };
    }

    BattleLogEntry? IBattleEventVisitor<BattleState, BattleLogEntry?>.Visit(BattleUnitEffectAppliedEvent ev, BattleState context)
    {
        return new BattleLogEventEntry(TrSpans("{0:*} is affected by {1:i}.", Unit(ev.TargetId, context), BattleStatusEffectFormatter.FormatName(ev.Effect)));
    }

    BattleLogEntry? IBattleEventVisitor<BattleState, BattleLogEntry?>.Visit(BattleUnitModifierAppliedEvent ev, BattleState context)
    {
        return new BattleLogEventEntry(TrSpans("{0:*} received {1:i}.", Unit(ev.UnitId, context), Modifier(ev.Modifier.DefinitionId)));
    }

    BattleLogEntry? IBattleEventVisitor<BattleState, BattleLogEntry?>.Visit(BattleUnitEffectDurationDecreasedEvent ev, BattleState context)
    {
        return null;
    }

    BattleLogEntry? IBattleEventVisitor<BattleState, BattleLogEntry?>.Visit(BattleUnitEffectExpiredEvent ev, BattleState context)
    {
        return new BattleLogEventEntry(TrSpans("{0:*} is no longer affected by {1:i}.", Unit(ev.UnitId, context), BattleStatusEffectFormatter.FormatName(ev.Effect)));
    }

    BattleLogEntry? IBattleEventVisitor<BattleState, BattleLogEntry?>.Visit(BattleUnitTurnStartedEvent ev, BattleState context)
    {
        return null;
    }

    BattleLogEntry? IBattleEventVisitor<BattleState, BattleLogEntry?>.Visit(BattleUnitTurnSkippedEvent ev, BattleState context)
    {
        return new BattleLogEventEntry(TrSpans("{0:*} skipped a turn.", Unit(ev.UnitId, context)));
    }

    BattleLogEntry? IBattleEventVisitor<BattleState, BattleLogEntry?>.Visit(BattleUnitTurnEndingEvent ev, BattleState context)
    {
        return null;
    }

    BattleLogEntry? IBattleEventVisitor<BattleState, BattleLogEntry?>.Visit(BattleUnitTurnEndedEvent ev, BattleState context)
    {
        return null;
    }

    BattleLogEntry? IBattleEventVisitor<BattleState, BattleLogEntry?>.Visit(BattleUnitWaitedEvent ev, BattleState context)
    {
        return new BattleLogEventEntry(TrSpans("{0:*} waited.", Unit(ev.UnitId, context)));
    }

    BattleLogEntry? IBattleEventVisitor<BattleState, BattleLogEntry?>.Visit(BattleUnitWardConsumedEvent ev, BattleState context)
    {
        return new BattleLogEventEntry(TrSpans("{0:*} used the {1:i} ward.", Unit(ev.UnitId, context), UnitWardFormatter.FormatName(ev.Ward)));
    }

    BattleLogEntry? IBattleEventVisitor<BattleState, BattleLogEntry?>.Visit(BattleUnitWardGrantedEvent ev, BattleState context)
    {
        return new BattleLogEventEntry(TrSpans("{0:*} gained the {1:i} ward.", Unit(ev.UnitId, context), UnitWardFormatter.FormatName(ev.Modifier.Ward)));
    }

    BattleLogEntry? IBattleEventVisitor<BattleState, BattleLogEntry?>.Visit(BattleUnitWardExpiredEvent ev, BattleState context)
    {
        return new BattleLogEventEntry(TrSpans("{0:*} lost the {1:i} ward.", Unit(ev.UnitId, context), UnitWardFormatter.FormatName(ev.Modifier.Ward)));
    }

    BattleLogEntry? IBattleEventVisitor<BattleState, BattleLogEntry?>.Visit(BattleUnitRetreatingEvent ev, BattleState context)
    {
        return new BattleLogEventEntry(TrSpans("{0:*} started retreating.", Unit(ev.UnitId, context)));
    }

    BattleLogEntry? IBattleEventVisitor<BattleState, BattleLogEntry?>.Visit(BattleUnitRetreatedEvent ev, BattleState context)
    {
        return new BattleLogEventEntry(TrSpans("{0:*} retreated.", Unit(ev.UnitId, context)));
    }

    private string Unit(BattleUnitId id, BattleState state)
    {
        return UnitNameResolver.Resolve(state.GetUnit(id), definitions, texts);
    }

    private string UnitType(UnitDefinitionId id)
    {
        UnitDefinition unit = definitions.GetUnit(id);

        return Text(unit.NameTextId);
    }

    private string Item(ItemDefinitionId id)
    {
        ItemDefinition item = definitions.GetItem(id);

        return Text(item.NameTextId);
    }

    private string Modifier(UnitModifierDefinitionId id)
    {
        UnitModifierDefinition modifier = definitions.GetUnitModifier(id);

        return Text(modifier.DescriptionTextId);
    }

    private string Text(GlobalTextId id)
    {
        GlobalText text = texts.GetGlobalText(id);

        return text.Text;
    }
}
