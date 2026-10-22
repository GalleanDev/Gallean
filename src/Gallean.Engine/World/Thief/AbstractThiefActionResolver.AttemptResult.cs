using System.Collections.Immutable;

namespace Gallean.Engine;

internal abstract partial class AbstractThiefActionResolver
{
    protected sealed record AttemptResult(
        WorldCommandError? Error,
        WorldThiefActionType Type,
        WorldPlayerId? TargetPlayerId,
        ImmutableArray<WorldEvent> Events)
    {
        public static AttemptResult Fail(WorldCommandError error)
        {
            return new AttemptResult(error, default, null, []);
        }

        public static AttemptResult Success(
            WorldThiefActionType type,
            WorldPlayerId? targetPlayerId,
            IEnumerable<WorldEvent> events)
        {
            return new AttemptResult(null, type, targetPlayerId, [.. events]);
        }
    }
}
