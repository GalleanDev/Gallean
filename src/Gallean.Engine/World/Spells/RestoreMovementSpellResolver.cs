using Gallean.Common;

namespace Gallean.Engine;

internal sealed class RestoreMovementSpellResolver : WorldAbstractUnitSpellResolver<WorldRestoreMovementSpellEffect>, IStaticInstance<RestoreMovementSpellResolver>
{
    protected override bool IsTarget(
        WorldState state,
        IGameDefinitionProvider definitions,
        WorldPlayerId casterId,
        WorldStack stack,
        WorldSpellDefinition spell)
    {
        return stack.OwnerId == casterId;
    }

    protected override WorldCommandResult OnHit(
        IEngineContext context,
        WorldPlayerId casterId,
        WorldStack stack,
        int powerPercent,
        WorldRestoreMovementSpellEffect effect)
    {
        StackStats stats = context.UnitStats.GetWorldStackStats(stack);
        int percent = effect.Percent * powerPercent / 100;
        int restoredMovementPoints = stats.MaxMovementPoints * percent / 100;
        int updatedMovementPoints = Math.Min(stack.MovementPoints + restoredMovementPoints, stats.MaxMovementPoints);

        return WorldCommandResult.Success(
            Empty.Value,
            [new WorldStackMovementRestoredEvent(stack.Id, updatedMovementPoints)]);
    }
}
