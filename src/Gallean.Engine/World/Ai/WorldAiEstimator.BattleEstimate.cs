namespace Gallean.Engine;

internal sealed partial class WorldAiEstimator
{
    public sealed record BattleEstimate(
        BattleOutcome Outcome,
        int UnitCountBefore,
        int UnitCountAfter,
        int HitpointsBefore,
        int HitpointsAfter,
        int ExperienceBefore,
        int ExperienceLost,
        int ExperienceGained,
        bool IsLeaderAlive)
    {
        public bool IsWinner => Outcome.WinningSide == BattleSide.Attacker;
        public int UnitsLost => Math.Max(0, UnitCountBefore - UnitCountAfter);
        public int HitpointsLost => Math.Max(0, HitpointsBefore - HitpointsAfter);
    }
}
