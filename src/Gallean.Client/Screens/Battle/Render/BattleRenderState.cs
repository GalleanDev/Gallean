using Avalonia;
using Avalonia.Media;
using Gallean.Common;

namespace Gallean.Client;

internal sealed record BattleRenderState(
    AssetSpriteKey.BattleBackground Background,
    IReadOnlyList<BattleRenderState.Unit> Units,
    IReadOnlyList<BattleRenderState.Placeholder> Placeholders,
    IReadOnlyList<BattleRenderState.HitTarget> HitTargets,
    BattleLightConfiguration LightConfiguration,
    IReadOnlyList<BattleFluidConfiguration> FluidConfigurations,
    BattleRainConfiguration? RainConfiguration,
    AnimationRate RedrawRate)
{
    public sealed record HitTarget(BattleGridTile Tile, Rect Bounds);

    public sealed record Placeholder(BattleGridTile Tile, Color? Marker);

    public sealed record Unit(
        BattleUnitId Id,
        UnitDefinitionId DefinitionId,
        WorldRace Race,
        UnitDeathAnimation DeathAnimation,
        BattleGridFootprint Footprint,
        BattleSide Facing,
        TimeSpan IdleOffset,
        int CorpseVariant,
        int Hitpoints,
        int MaxHitpoints,
        BattleUnitState.Presence Presence,
        Color? Marker,
        Color? Highlight,
        BattleUnitState.Auras Auras,
        bool Frozen)
    {
        public bool Equals(Unit? other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return Id == other.Id &&
                   DefinitionId == other.DefinitionId &&
                   Race == other.Race &&
                   DeathAnimation == other.DeathAnimation &&
                   Footprint == other.Footprint &&
                   Facing == other.Facing &&
                   IdleOffset == other.IdleOffset &&
                   CorpseVariant == other.CorpseVariant &&
                   Hitpoints == other.Hitpoints &&
                   MaxHitpoints == other.MaxHitpoints &&
                   Presence == other.Presence &&
                   Marker == other.Marker &&
                   Highlight == other.Highlight &&
                   Auras == other.Auras &&
                   Frozen == other.Frozen;
        }

        public override int GetHashCode()
        {
            HashCode hash = new();

            hash.Add(Id);
            hash.Add(DefinitionId);
            hash.Add(Race);
            hash.Add(DeathAnimation);
            hash.Add(Footprint);
            hash.Add(Facing);
            hash.Add(IdleOffset);
            hash.Add(CorpseVariant);
            hash.Add(Hitpoints);
            hash.Add(MaxHitpoints);
            hash.Add(Presence);
            hash.Add(Marker);
            hash.Add(Highlight);
            hash.Add(Auras);
            hash.Add(Frozen);

            return hash.ToHashCode();
        }
    }

    public bool Equals(BattleRenderState? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return Background == other.Background &&
               Units.SequenceEqual(other.Units) &&
               Placeholders.SequenceEqual(other.Placeholders) &&
               HitTargets.SequenceEqual(other.HitTargets) &&
               LightConfiguration == other.LightConfiguration &&
               FluidConfigurations.SequenceEqual(other.FluidConfigurations) &&
               RainConfiguration == other.RainConfiguration &&
               RedrawRate == other.RedrawRate;
    }

    public override int GetHashCode()
    {
        HashCode hash = new();

        hash.Add(Background);
        hash.AddSequence(Units);
        hash.AddSequence(Placeholders);
        hash.AddSequence(HitTargets);
        hash.Add(LightConfiguration);
        hash.AddSequence(FluidConfigurations);
        hash.Add(RainConfiguration);
        hash.Add(RedrawRate);

        return hash.ToHashCode();
    }
}
