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

    public static int GetArmorBonus(WorldFortification fortification, GlobalVariables globals)
    {
        return fortification.Accept(ArmorVisitor.Instance, globals);
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

    private static int GetCityArmorBonus(int cityLevel, GlobalVariables globals)
    {
        return cityLevel switch
        {
            1 => globals.ProtectionCityLevel1,
            2 => globals.ProtectionCityLevel2,
            3 => globals.ProtectionCityLevel3,
            4 => globals.ProtectionCityLevel4,
            5 => globals.ProtectionCityLevel5,
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
