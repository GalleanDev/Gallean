namespace Gallean.Engine;

internal partial class BattleEngine
{
    private sealed class AwardRule(IEngineContext context, IBattleUnitUpgradeResolver upgradeResolver) : BattleEventRule(context)
    {
        public override EventRuleResult<BattleEvent> Visit(BattleEndedEvent ev, BattleState state)
        {
            return EventRuleResult<BattleEvent>.Append(
            [
                .. ProcessSide(state, BattleSide.Attacker, ev.WinningSide == BattleSide.Attacker),
                .. ProcessSide(state, BattleSide.Defender, ev.WinningSide == BattleSide.Defender)
            ]);
        }

        private IEnumerable<BattleEvent> ProcessSide(BattleState state, BattleSide side, bool isWinner)
        {
            UnitGrid<BattleUnit> grid = state.GetGrid(side);
            int sideBonusPercent = state.Modifiers.GetModifiers(side).ExperiencePercent;

            int leaderBonusPercent = 0;

            if (isWinner)
            {
                leaderBonusPercent = GetExperienceBonusPercent(state, grid);
            }

            foreach ((BattleUnit unit, _) in grid.GetUnits())
            {
                UnitExperience unitExperience = Context.UnitStats.GetBattleUnitExperience(state, unit.Id);

                if (unit.SummonerId is null &&
                    unit.IsAlive &&
                    unitExperience.CanGainExperience)
                {
                    int experience = GetExperienceGained(unit, isWinner, leaderBonusPercent, sideBonusPercent);

                    if (experience > 0)
                    {
                        yield return CreateExperienceGainedEvent(unit, unitExperience, experience);
                    }
                }
            }
        }

        private static int GetExperienceGained(BattleUnit unit, bool isWinner, int leaderBonusPercent, int sideBonusPercent)
        {
            int experience = unit.ExperienceGained;

            if (isWinner && unit.IsActive())
            {
                experience += experience * leaderBonusPercent / 100;
                experience += experience * sideBonusPercent / 100;

                return Math.Max(1, experience);
            }
            else
            {
                return experience;
            }
        }

        private BattleEvent CreateExperienceGainedEvent(BattleUnit unit, UnitExperience unitExperience, int experience)
        {
            if (unitExperience.NextLevel > 0 && unitExperience.Current + experience >= unitExperience.NextLevel)
            {
                if (unit.IsActive())
                {
                    BattleUnitUpgrade? upgrade = upgradeResolver.UpgradeUnit(unit);

                    if (upgrade is not null)
                    {
                        return new BattleUnitUpgradedEvent(unit.Id, unit.DefinitionId, upgrade.DefinitionId, upgrade.Level, upgrade.Hitpoints, upgrade.MaxHitpoints);
                    }
                }

                experience = Math.Max(0, Math.Min(experience, unitExperience.NextLevel - 1 - unitExperience.Current));
            }

            return new BattleUnitExperienceGainedEvent(unit.Id, experience);
        }

        private int GetExperienceBonusPercent(BattleState state, UnitGrid<BattleUnit> grid)
        {
            foreach ((BattleUnit unit, _) in grid.GetUnits())
            {
                UnitDefinition definition = Context.Definitions.GetUnit(unit.DefinitionId);

                if (definition.Leadership is > 0 &&
                    Context.UnitStats.GetBattleUnitAbilities(state, unit.Id).HasFlag(LeaderAbility.WeaponMaster))
                {
                    return Context.Globals.ExperienceBonusWeaponMaster;
                }
            }

            return 0;
        }
    }
}
