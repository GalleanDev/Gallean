using System.Collections.Immutable;
using Gallean.Common;

namespace Gallean.Engine;

internal static partial class FortificationCalculator
{
    public const int CapitalFootprint = 5;
    public const int CityFootprint = 4;
    public const int RuinFootprint = 3;
    public const int SiteFootprint = 3;

    private static readonly ImmutableArray<(int X, int Y)> ExitOffsets = [(1, 1), (0, 1), (1, 0), (-1, 1), (1, -1)];

    public static WorldFootprint CreateFootprint(WorldPosition entrance, int size)
    {
        return new WorldFootprint(new WorldPosition(entrance.X - size + 1, entrance.Y - size + 1), size, size);
    }

    public static WorldPosition GetEntrance(WorldFootprint footprint)
    {
        return new WorldPosition(footprint.TopLeft.X + footprint.Width - 1, footprint.TopLeft.Y + footprint.Height - 1);
    }

    public static WorldPosition GetCenter(WorldFootprint footprint)
    {
        return new WorldPosition(footprint.TopLeft.X + footprint.Width / 2, footprint.TopLeft.Y + footprint.Height / 2);
    }

    public static WorldPlayerId? GetOwner(WorldFortification fortification)
    {
        return fortification.Accept(OwnerVisitor.Instance, Empty.Value);
    }

    public static WorldPosition? FindExit(
        WorldFortification fortification,
        WorldStack stack,
        WorldState state,
        IGameDefinitionProvider definitions)
    {
        WorldPlan plan = WorldPlan.For(state);
        WorldMovementCalculator.MovementProfile profile = WorldMovementCalculator.GetMovementProfile(stack, definitions);
        WorldPosition entrance = GetEntrance(fortification.Footprint);

        foreach ((int x, int y) in ExitOffsets)
        {
            WorldPosition position = new(entrance.X + x, entrance.Y + y);

            if (plan.CanPlaceStackAt(position, profile))
            {
                return position;
            }
        }

        return null;
    }

    public static bool CanCapture(WorldStack stack, IGameDefinitionProvider definitions)
    {
        (_, _, UnitDefinition leaderDefinition) = stack.GetLeader(definitions);

        if (leaderDefinition.Category is UnitCategory.Summon or UnitCategory.Illusion)
        {
            return false;
        }
        else if (leaderDefinition.LeaderCategory is UnitLeaderCategory.Thief)
        {
            return false;
        }
        else
        {
            return true;
        }
    }

    public static int GetArmorBonus(WorldFortification fortification, WorldState state, IGameDefinitionProvider definitions)
    {
        UnitModifierDefinitionId? modifierId = GetProtectionModifierId(fortification, state, definitions);

        if (modifierId is not null)
        {
            UnitModifierDefinition modifier = definitions.GetUnitModifier(modifierId.Value);

            return modifier.Armor.Flat;
        }
        else
        {
            return 0;
        }
    }

    public static bool IsRioting(WorldFortification fortification)
    {
        return fortification.Accept(RiotVisitor.Instance, Empty.Value);
    }

    public static int GetThiefProtection(WorldFortification fortification, GlobalVariables globals)
    {
        return fortification.Accept(ThiefProtectionVisitor.Instance, globals);
    }

    public static int GetVisionRadius(WorldFortification fortification, IEngineContext context)
    {
        return fortification.Accept(VisionRadiusVisitor.Instance, context);
    }

    public static int GetTerrainGrowth(WorldFortification fortification, GlobalVariables globals)
    {
        return fortification.Accept(TerrainGrowthVisitor.Instance, globals);
    }

    public static int GetCaptureTerrainGrowth(WorldFortification fortification, GlobalVariables globals)
    {
        return fortification.Accept(CaptureTerrainGrowthVisitor.Instance, globals);
    }

    public static WorldState UpdateUnitModifiers(WorldState state, IGameDefinitionProvider definitions)
    {
        WorldPlan plan = WorldPlan.For(state);
        WorldState result = state;

        foreach (WorldStack stack in state.Stacks.Values)
        {
            WorldFortification? fortification = plan.GetFortificationAt(stack.Position);

            UnitModifierDefinitionId? modifierId;
            if (fortification is not null)
            {
                modifierId = GetProtectionModifierId(fortification, state, definitions);
            }
            else
            {
                modifierId = null;
            }

            if (!HasProtection(stack.Units, modifierId))
            {
                UnitGrid<WorldUnit> units = SetProtection(stack.Units, modifierId);
                result = result with { Stacks = result.Stacks.SetItem(stack.Id, stack with { Units = units }) };
            }
        }

        foreach (WorldFortification fortification in state.Fortifications.Values)
        {
            UnitModifierDefinitionId? modifierId = GetProtectionModifierId(fortification, state, definitions);

            if (!HasProtection(fortification.Garrison, modifierId))
            {
                UnitGrid<WorldUnit> garrison = SetProtection(fortification.Garrison, modifierId);
                result = result with { Fortifications = result.Fortifications.SetItem(fortification.Id, fortification with { Garrison = garrison }) };
            }
        }

        return result;
    }

    public static UnitGrid<WorldUnit> RemoveUnitModifiers(UnitGrid<WorldUnit> units)
    {
        return SetProtection(units, modifierId: null);
    }

    private static bool HasProtection(UnitGrid<WorldUnit> units, UnitModifierDefinitionId? modifierId)
    {
        foreach ((WorldUnit unit, _) in units.GetUnits())
        {
            if (GetProtection(unit) != modifierId)
            {
                return false;
            }
        }

        return true;
    }

    private static UnitModifierDefinitionId? GetProtection(WorldUnit unit)
    {
        foreach (UnitModifier modifier in unit.Modifiers)
        {
            if (modifier.Source is UnitModifierSource.Fortification)
            {
                return modifier.DefinitionId;
            }
        }

        return null;
    }

    private static UnitGrid<WorldUnit> SetProtection(UnitGrid<WorldUnit> units, UnitModifierDefinitionId? modifierId)
    {
        UnitGrid<WorldUnit> result = units;

        foreach ((WorldUnit unit, _) in units.GetUnits())
        {
            ImmutableArray<UnitModifier> modifiers = unit.Modifiers.RemoveAll(modifier => modifier.Source is UnitModifierSource.Fortification);

            if (modifierId is not null)
            {
                modifiers = modifiers.Add(UnitModifier.FromFortification(modifierId.Value));
            }

            result = result.Replace(unit, unit with { Modifiers = modifiers });
        }

        return result;
    }

    private static UnitModifierDefinitionId? GetProtectionModifierId(
        WorldFortification fortification,
        WorldState state,
        IGameDefinitionProvider definitions)
    {
        return fortification.Accept(ProtectionModifierVisitor.Instance, new ProtectionContext(state, definitions));
    }

    private static int GetCityTerrainGrowth(int cityLevel, GlobalVariables globals)
    {
        return cityLevel switch
        {
            1 => globals.CityTerrainTilesPerTurnLevel1,
            2 => globals.CityTerrainTilesPerTurnLevel2,
            3 => globals.CityTerrainTilesPerTurnLevel3,
            4 => globals.CityTerrainTilesPerTurnLevel4,
            5 => globals.CityTerrainTilesPerTurnLevel5,
            _ => 0
        };
    }

    private static int GetCityThiefProtection(int cityLevel, GlobalVariables globals)
    {
        return cityLevel switch
        {
            1 => globals.ThiefProtectionCityLevel1,
            2 => globals.ThiefProtectionCityLevel2,
            3 => globals.ThiefProtectionCityLevel3,
            4 => globals.ThiefProtectionCityLevel4,
            5 => globals.ThiefProtectionCityLevel5,
            6 => globals.ThiefProtectionCityLevel6,
            _ => 0
        };
    }
}
