using System.Diagnostics.CodeAnalysis;

namespace Gallean.Engine;

internal sealed record BattleAttackResult(IReadOnlyList<BattleEvent> Events, bool DidHit, BattleCommandError? Error)
{
    [MemberNotNullWhen(true, nameof(Error))]
    public bool Failed => Error is not null;

    public static BattleAttackResult Fail(BattleCommandError error)
    {
        return new BattleAttackResult([], DidHit: false, error);
    }
}
