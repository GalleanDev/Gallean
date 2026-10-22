namespace Gallean.Engine;

internal interface IWorldDiplomacyActionResolver
{
    WorldCommandResult Resolve(WorldState state, IEngineContext context, WorldPlayerId player, WorldPlayerId targetPlayer);
}
