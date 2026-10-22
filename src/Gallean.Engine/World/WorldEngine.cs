namespace Gallean.Engine;

internal sealed partial class WorldEngine(IEngineContext context, WorldState initialState) :
    AbstractEngine<WorldState, WorldCommandError, WorldEvent>(context, initialState, CreateRules),
    IWorldEventVisitor<WorldState, WorldState>
{
    private static IReadOnlyList<IEventRule<WorldState, WorldEvent>> CreateRules(IEngineContext context)
    {
        return
        [
            new BattleEndedLootRuinRule(context),
            new BattleEndedDestroyStackRule(context),
            new BattleEndedRetreatStackRule(context),
            new UnitDamagedDestroyStackRule(context),
            new BattleEndedCaptureFortificationRule(context),
            new FortificationCapturingReceiveSpellsRule(context),
            new FortificationCapturingRiotRule(context),
            new FortificationCapturingDefeatRule(context),
            new PlayerDefeatedDestroyStacksRule(context),
            new PlayerDefeatedEndScenarioRule(context),
            new FogConcealedRevealVisionRule(context),
            new StackMovingRevealFogRule(context),
            new StackMovingFortificationRule(context),
            new StackMovingCaptureFortificationRule(context),
            new StackMovedStepRule(context),
            new StackActionRevealRule(context),
            new DiplomacyProposalSentAiAnswerRule(context),
            new DiplomacyProposalAcceptedRule(context),
            new DiplomacyRelationRule(context),
            new AllianceStartedAddSpiesRule(context),
            new AllianceEndedRemoveSpiesRule(context),
            new SpellCastEffectRule(context),
            new ScenarioEventTriggeringEffectRule(context),
            new TurnEndedEndRoundRule(context),
            new RoundEndedStartRoundRule(context),
            new TurnStartingCaptureNodeRule(context),
            new TurnStartingDiscoverSpiesRule(context),
            new TurnStartingRestoreMovementRule(context),
            new TurnStartingUnitModifierDurationRule(context),
            new TurnStartingStackExpirationRule(context),
            new TurnStartingCityRiotRule(context),
            new TurnStartedGenerateResourcesRule(context),
            new TurnStartedUpgradeAiLeadersRule(context),
            new TriggerScenarioEventsRule(context)
        ];
    }

    protected override WorldState ApplyEvent(WorldEvent ev, WorldState state)
    {
        return ev.Accept(this, state);
    }
}
