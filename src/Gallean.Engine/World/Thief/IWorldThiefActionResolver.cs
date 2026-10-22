namespace Gallean.Engine;

internal interface IWorldThiefActionResolver
{
    WorldCommandResult Resolve(WorldState state, IEngineContext context, WorldStack thief, WorldPlayerId player);
}
