using Gallean.Common;
using Gallean.Engine.Contracts;
using static GameContent.GameAssetDescriptor;
using static GameContent.GameResourceNames;

namespace GameContent;

internal sealed class GameStackSource(GameImages units, GameImages common, GameGraphicsResolver graphicsResolver)
{
    public static IEnumerable<GameAssetDescriptor> ListDescriptors(IReadOnlyCollection<UnitDefinitionId> unitIds)
    {
        yield return RequiredAnimation(
            new AssetAnimationKey.StackDestroyEffect(),
            source => AnimationOperations.TryCreate(source.Stacks.GetDestroyFrames()));

        yield return RequiredAnimation(
            new AssetAnimationKey.StackSummonDestroyEffect(),
            source => AnimationOperations.TryCreate(source.Stacks.GetSummonDestroyFrames()));

        yield return RequiredAnimation(
            new AssetAnimationKey.StackCreateEffect(),
            source => AnimationOperations.TryCreate(source.Stacks.GetCreateFrames()));

        yield return RequiredAnimation(
            new AssetAnimationKey.StackOwnerChangeEffect(),
            source => AnimationOperations.TryCreate(source.Stacks.GetChangeOwnerFrames()));

        yield return RequiredAnimation(
            new AssetAnimationKey.StackBattleEffect(),
            source => AnimationOperations.TryCreate(source.Stacks.GetBattleFrames()));

        foreach (WorldRace race in Enum.GetValues<WorldRace>())
        {
            foreach (AssetAnimationKey.StackUnitPose pose in Enum.GetValues<AssetAnimationKey.StackUnitPose>())
            {
                for (int direction = 0; direction < AssetAnimationKey.DirectionCount; direction++)
                {
                    AssetAnimationKey.StackBoat boat = new(race, pose, direction);
                    AssetAnimationKey.StackBoatShadow shadow = new(race, pose, direction);

                    yield return RequiredAnimation(
                        boat,
                        source => AnimationOperations.TryCreate(source.Stacks.GetBoatFrames(boat.Race, boat.Pose, boat.Direction)));

                    yield return OptionalAnimation(
                        shadow,
                        source => source.Stacks.GetBoatShadow(shadow.Race, shadow.Pose, shadow.Direction));
                }
            }
        }

        foreach (UnitDefinitionId unitId in unitIds)
        {
            foreach (AssetAnimationKey.StackUnitPose pose in Enum.GetValues<AssetAnimationKey.StackUnitPose>())
            {
                for (int direction = 0; direction < AssetAnimationKey.DirectionCount; direction++)
                {
                    AssetAnimationKey.StackUnit unit = new(unitId, pose, direction);
                    AssetAnimationKey.StackUnitAura aura = new(unitId, pose, direction);
                    AssetAnimationKey.StackUnitShadow shadow = new(unitId, pose, direction);

                    yield return OptionalAnimation(
                        unit,
                        source => AnimationOperations.TryCreate(source.Stacks.GetUnitFrames(unit.DefinitionId, unit.Pose, unit.Direction)));

                    yield return OptionalAnimation(
                        aura,
                        source => source.Stacks.GetUnitAura(aura.DefinitionId, aura.Pose, aura.Direction));

                    yield return OptionalAnimation(
                        shadow,
                        source => source.Stacks.GetUnitShadow(shadow.DefinitionId, shadow.Pose, shadow.Direction));
                }
            }
        }
    }

    private IReadOnlyList<AssetSprite>? GetDestroyFrames()
    {
        IReadOnlyList<AssetSprite>? frames = common.TryGetAnimationFrames(Stacks.Destroy);

        return frames is { Count: > 0 } ? SpriteOperations.CropFramesToContent(frames, SpriteOperations.ContentOrigin.Center, margin: 0) : null;
    }

    private IReadOnlyList<AssetSprite>? GetSummonDestroyFrames()
    {
        IReadOnlyList<AssetSprite>? frames = common.TryGetAnimationFrames(Stacks.SummonDestroy);

        return frames is { Count: > 0 } ? SpriteOperations.CropFramesToContent(frames, SpriteOperations.ContentOrigin.Center, margin: 0) : null;
    }

    private IReadOnlyList<AssetSprite>? GetCreateFrames()
    {
        IReadOnlyList<AssetSprite>? frames = common.TryGetAnimationFrames(Stacks.Create);

        return frames is { Count: > 0 } ? SpriteOperations.CropFramesToContent(frames, SpriteOperations.ContentOrigin.Center, margin: 0) : null;
    }

    private IReadOnlyList<AssetSprite>? GetChangeOwnerFrames()
    {
        IReadOnlyList<AssetSprite>? frames = common.TryGetAnimationFrames(Stacks.ChangeOwner);

        return frames is { Count: > 0 } ? SpriteOperations.CropFramesToContent(frames, SpriteOperations.ContentOrigin.Center, margin: 0) : null;
    }

    private IReadOnlyList<AssetSprite>? GetBattleFrames()
    {
        IReadOnlyList<AssetSprite>? frames = common.TryGetAnimationFrames(Stacks.Battle);

        return frames is { Count: > 0 } ? SpriteOperations.CropFramesToContent(frames, SpriteOperations.ContentOrigin.Center, margin: 0) : null;
    }

    private IReadOnlyList<AssetSprite>? GetUnitFrames(UnitDefinitionId unitId, AssetAnimationKey.StackUnitPose pose, int direction)
    {
        UnitDefinitionId graphicsId = graphicsResolver.ResolveUnitMapGraphicsId(unitId);

        return GetStackFrames(Stacks.Unit(graphicsId.Value, pose, direction.Mod(8)));
    }

    private IReadOnlyList<AssetSprite>? GetBoatFrames(WorldRace race, AssetAnimationKey.StackUnitPose pose, int direction)
    {
        return GetStackFrames(Stacks.Boat(GetBoatVariant(race), pose, direction.Mod(8)));
    }

    private AssetAnimation? GetUnitShadow(UnitDefinitionId unitId, AssetAnimationKey.StackUnitPose pose, int direction)
    {
        UnitDefinitionId graphicsId = graphicsResolver.ResolveUnitMapGraphicsId(unitId);
        IReadOnlyList<AssetSprite>? shadows = GetStackFrames(Stacks.UnitShadow(graphicsId.Value, pose, direction.Mod(8)));
        IReadOnlyList<AssetSprite>? bodies = GetStackFrames(Stacks.Unit(graphicsId.Value, pose, direction.Mod(8)));

        if (shadows is not null && bodies is not null)
        {
            return AnimationOperations.Create(shadows, triggerFrame: null, secondTriggerFrame: null);
        }
        else
        {
            return null;
        }
    }

    private AssetAnimation? GetUnitAura(UnitDefinitionId unitId, AssetAnimationKey.StackUnitPose pose, int direction)
    {
        UnitDefinitionId graphicsId = graphicsResolver.ResolveUnitMapGraphicsId(unitId);

        return BuildLayer(Stacks.UnitAura(graphicsId.Value, pose, direction.Mod(8)));
    }

    private AssetAnimation? GetBoatShadow(WorldRace race, AssetAnimationKey.StackUnitPose pose, int direction)
    {
        return BuildLayer(Stacks.BoatShadow(GetBoatVariant(race), pose, direction.Mod(8)));
    }

    private static int GetBoatVariant(WorldRace race)
    {
        return race switch
        {
            WorldRace.Human => 0,
            WorldRace.Undead => 1,
            WorldRace.Heretic => 2,
            WorldRace.Dwarf => 3,
            WorldRace.Neutral => 4,
            WorldRace.Elf => 5
        };
    }

    private IReadOnlyList<AssetSprite>? GetStackFrames(string name)
    {
        IReadOnlyList<AssetSprite>? frames = units.TryGetAnimationFrames(name);

        return frames is { Count: > 0 } ? SpriteOperations.CropFramesToContent(frames, SpriteOperations.ContentOrigin.Center, margin: 0) : null;
    }

    private AssetAnimation? BuildLayer(string layerName)
    {
        IReadOnlyList<AssetSprite>? layerFrames = units.TryGetAnimationFrames(layerName);

        if (layerFrames is { Count: > 0 })
        {
            IReadOnlyList<AssetSprite> frames = SpriteOperations.CropFramesToContent(
                layerFrames,
                SpriteOperations.ContentOrigin.Center,
                margin: 0);

            return AnimationOperations.Create(frames, triggerFrame: null, secondTriggerFrame: null);
        }
        else
        {
            return null;
        }
    }
}
