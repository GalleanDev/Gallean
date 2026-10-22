using Gallean.Engine.Contracts;

namespace Packer;

internal static class FluidMap
{
    private const int CitySalt = 100;
    private const int RuinSalt = 200;
    private const int CapitalSalt = 300;
    private const int BoatSalt = 400;
    private const int Variants = 7;
    private const int LayerSalt = 10000;

    public static IReadOnlyList<UmbraFluidConfiguration> GetConfigurations(AssetSpriteKey.BattleBackground key)
    {
        return (key.Location, key.Variant) switch
        {
            (AssetSpriteKey.BattleLocation.Terrain { Value: WorldMapTerrain.Heretic }, _) => [MakeLiquidConfiguration(GetSeed(key, 0)), MakeGasConfiguration(GetSeed(key, 1))],
            (AssetSpriteKey.BattleLocation.Terrain, _) => [MakeGasConfiguration(GetSeed(key, 0))],
            (AssetSpriteKey.BattleLocation.City, _) => [MakeGasConfiguration(GetSeed(key, 0))],
            (AssetSpriteKey.BattleLocation.Ruin, _) => [MakeGasConfiguration(GetSeed(key, 0))],
            (AssetSpriteKey.BattleLocation.Capital { Race: WorldRace.Heretic or WorldRace.Dwarf }, _) => [MakeLiquidConfiguration(GetSeed(key, 0)), MakeGasConfiguration(GetSeed(key, 1))],
            (AssetSpriteKey.BattleLocation.Capital { Race: WorldRace.Elf }, _) => [MakeLiquidConfiguration(GetSeed(key, 0))],
            (AssetSpriteKey.BattleLocation.Capital, _) => [MakeGasConfiguration(GetSeed(key, 0))],
            (AssetSpriteKey.BattleLocation.Boat, 0 or 1) => [MakeGasConfiguration(GetSeed(key, 0))],
            (AssetSpriteKey.BattleLocation.Boat, _) => [],
            _ => throw new ArgumentOutOfRangeException(nameof(key), key, null)
        };
    }

    private static int GetSeed(AssetSpriteKey.BattleBackground key, int layer)
    {
        int location = key.Location switch
        {
            AssetSpriteKey.BattleLocation.Terrain terrain => (int)terrain.Value,
            AssetSpriteKey.BattleLocation.City => CitySalt,
            AssetSpriteKey.BattleLocation.Ruin => RuinSalt,
            AssetSpriteKey.BattleLocation.Capital capital => CapitalSalt + (int)capital.Race,
            AssetSpriteKey.BattleLocation.Boat => BoatSalt,
            _ => throw new ArgumentOutOfRangeException(nameof(key), key, null)
        };

        return (layer * LayerSalt) + (location * Variants) + key.Variant;
    }

    private static UmbraFluidConfiguration MakeLiquidConfiguration(int seed)
    {
        return new UmbraFluidConfiguration(
            UmbraFluidMedium.MakeLiquid(),
            seed,
            SimulationScale: 0.4,
            RestoreReach: 0.633,
            SourceThreshold: 0.0,
            SourceContrast: 100.0,
            SourceFrequency: 12.6,
            WarmUpSteps: 120,
            FrameCount: 72);
    }

    private static UmbraFluidConfiguration MakeGasConfiguration(int seed)
    {
        return new UmbraFluidConfiguration(
            UmbraFluidMedium.MakeGas(),
            seed,
            SimulationScale: 0.4,
            RestoreReach: 0.633,
            SourceThreshold: 0.30,
            SourceContrast: 3.5,
            SourceFrequency: 12.6,
            WarmUpSteps: 120,
            FrameCount: 72);
    }
}
