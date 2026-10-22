using Gallean.Common;
using Gallean.Engine;

namespace Gallean.Client;

internal sealed partial class BattleAssetPreloadPlanBuilder(
    BattleRenderState state,
    BattleState battle,
    IGameDefinitionProvider definitions,
    IAssetMetadataProvider assets)
{
    private readonly BattleAnimationResolver animationResolver = new(assets);
    private readonly HashSet<AssetKey> keys = [];

    public AssetPreloadPlan Build()
    {
        IReadOnlyList<UnitForm> present = ResolvePresentForms();
        IReadOnlyList<UnitForm> derived = ResolveDerivedForms(present);
        IReadOnlyList<UnitForm> upgrades = ResolveUpgradeForms(present);
        IReadOnlySet<AssetAnimationKey.BattleEffect.StatusType> statusTypes = ResolveStatusTypes(present, derived);

        List<AssetPreloadItem> initialRequired = [new AssetPreloadItem.Sprite(state.Background)];
        List<AssetPreloadItem> initialOptional = [];
        List<AssetPreloadItem> sounds = [];
        List<AssetPreloadItem> action = [];
        List<AssetPreloadItem> shared = [];
        List<AssetPreloadItem> summoned = [];
        List<AssetPreloadItem> retreated = [];
        List<AssetPreloadItem> resolved = [];

        foreach (BattleRenderState.Unit unit in state.Units)
        {
            AddPose(initialRequired, unit.DefinitionId, AssetAnimationKey.BattleUnitPose.Idle, unit.Facing);
            AddPose(initialRequired, unit.DefinitionId, AssetAnimationKey.BattleUnitPose.Attack, unit.Facing);
            AddPose(initialRequired, unit.DefinitionId, AssetAnimationKey.BattleUnitPose.Hit, unit.Facing);
        }

        for (int layer = 0; layer < state.FluidConfigurations.Count; layer++)
        {
            AddAnimation(initialOptional, new AssetAnimationKey.BattleBackgroundOverlay(state.Background.Location, state.Background.Variant, layer));
        }

        foreach (UnitForm form in present)
        {
            AddSprite(initialOptional, new AssetSpriteKey.UnitFace(form.DefinitionId));
            AddSprite(initialOptional, new AssetSpriteKey.UnitPortrait(form.DefinitionId));
            AddSprite(initialOptional, new AssetSpriteKey.UnitEventPortrait(ScenarioEventPortraitId.Unit(form.DefinitionId)));
        }

        foreach (UnitForm form in present)
        {
            AddUnitSounds(sounds, form.DefinitionId);
        }

        AddBattleEffectSounds(sounds);

        foreach (UnitForm form in present)
        {
            AddActionAnimations(action, form);
        }

        AddCorpses(shared);
        AddStatusEffects(shared, statusTypes);
        AddAnimation(shared, new AssetAnimationKey.BattleEffect.Heal());
        AddAnimation(shared, new AssetAnimationKey.BattleEffect.Blister());
        AddItemEffects(shared);

        foreach (UnitForm form in derived)
        {
            AddSprite(summoned, new AssetSpriteKey.UnitFace(form.DefinitionId));
            AddSprite(summoned, new AssetSpriteKey.UnitPortrait(form.DefinitionId));
            AddSprite(summoned, new AssetSpriteKey.UnitEventPortrait(ScenarioEventPortraitId.Unit(form.DefinitionId)));
            AddUnitSounds(summoned, form.DefinitionId);
            AddActionAnimations(summoned, form);
        }

        foreach (UnitForm form in present)
        {
            AddRetreatAnimations(retreated, form);
        }

        foreach (BattleRenderState.Unit unit in state.Units)
        {
            AddAnimation(resolved, new AssetAnimationKey.BattleEffect.LevelUp(unit.Race, unit.Footprint.IsLarge));
        }

        foreach (UnitForm form in upgrades)
        {
            AddSprite(resolved, new AssetSpriteKey.UnitFace(form.DefinitionId));
            AddSprite(resolved, new AssetSpriteKey.UnitPortrait(form.DefinitionId));
            AddSprite(resolved, new AssetSpriteKey.UnitEventPortrait(ScenarioEventPortraitId.Unit(form.DefinitionId)));
            AddUnitSounds(resolved, form.DefinitionId);
            AddActionAnimations(resolved, form);
        }

        return new AssetPreloadPlan(
            initialRequired,
            initialOptional,
            [.. sounds, .. action, .. shared, .. summoned, .. retreated, .. resolved]);
    }

    private void AddActionAnimations(List<AssetPreloadItem> items, UnitForm form)
    {
        AddPose(items, form.DefinitionId, AssetAnimationKey.BattleUnitPose.Idle, form.Facing);
        AddPose(items, form.DefinitionId, AssetAnimationKey.BattleUnitPose.Attack, form.Facing);
        AddPose(items, form.DefinitionId, AssetAnimationKey.BattleUnitPose.Hit, form.Facing);
        AddAttackEffects(items, form.DefinitionId);
        AddDeathEffect(items, form.DefinitionId);
    }

    private void AddRetreatAnimations(List<AssetPreloadItem> items, UnitForm form)
    {
        BattleSide facing = form.Facing.GetOpposite();

        AddPose(items, form.DefinitionId, AssetAnimationKey.BattleUnitPose.Idle, facing);
        AddPose(items, form.DefinitionId, AssetAnimationKey.BattleUnitPose.Hit, facing);
    }

    private void AddPose(
        List<AssetPreloadItem> items,
        UnitDefinitionId definitionId,
        AssetAnimationKey.BattleUnitPose pose,
        BattleSide facing)
    {
        AddAnimation(items, new AssetAnimationKey.BattleUnitShadow(definitionId, pose, facing));
        AddAnimation(items, new AssetAnimationKey.BattleUnit(definitionId, pose, facing));

        AssetAnimationKey.BattleUnitAura aura = new(definitionId, pose, facing);

        if (assets.GetOptionalAnimationMetadata(aura) is not null)
        {
            AddAnimation(items, aura);
        }
    }

    private void AddAttackEffects(List<AssetPreloadItem> items, UnitDefinitionId definitionId)
    {
        foreach (AssetAnimationKey.BattleEffect.AttackType type in Enum.GetValues<AssetAnimationKey.BattleEffect.AttackType>())
        {
            foreach (BattleSide targetSide in Enum.GetValues<BattleSide>())
            {
                IReadOnlyList<AssetAnimationKey.BattleEffect.Attack> layers = animationResolver.ResolveAttackLayers(definitionId, type, targetSide);

                foreach (int variant in animationResolver.ResolveAttackVariants(layers))
                {
                    foreach (AssetAnimationKey.BattleEffect.Attack layer in layers)
                    {
                        AddAnimation(items, layer.WithVariant(variant));
                    }
                }
            }
        }
    }

    private void AddDeathEffect(List<AssetPreloadItem> items, UnitDefinitionId definitionId)
    {
        UnitDefinition definition = definitions.GetUnit(definitionId);

        AddAnimation(items, animationResolver.ResolveDeath(definition.DeathAnimation, definition.IsLarge));
    }

    private void AddItemEffects(List<AssetPreloadItem> items)
    {
        foreach (BattleItem item in battle.Items)
        {
            ItemDefinition definition = definitions.GetItem(item.ItemId);
            ItemCategory category = definition.Category;

            if (animationResolver.ResolveItem(category) is { } effect)
            {
                AddAnimation(items, effect);
            }

            if (ItemSoundResolver.ResolveBattle(category) is { } sound)
            {
                AddSound(items, sound);
            }

            if (definition.AnimationUnitId is { } effectUnitId)
            {
                AddAttackEffects(items, effectUnitId);
                AddEffectSounds(items, effectUnitId);
            }
        }
    }

    private void AddEffectSounds(List<AssetPreloadItem> items, UnitDefinitionId definitionId)
    {
        for (int variant = 0; variant < AssetSoundKey.BattleUnitHit.VariantCount; variant++)
        {
            AddSound(items, new AssetSoundKey.BattleUnitHit(definitionId, variant));
        }

        for (int variant = 0; variant < AssetSoundKey.BattleUnitTouch.VariantCount; variant++)
        {
            AddSound(items, new AssetSoundKey.BattleUnitTouch(definitionId, variant));
        }
    }

    private void AddStatusEffects(List<AssetPreloadItem> items, IReadOnlySet<AssetAnimationKey.BattleEffect.StatusType> types)
    {
        foreach (bool isLarge in ResolveSizes())
        {
            foreach (AssetAnimationKey.BattleEffect.StatusType type in types)
            {
                AddAnimation(items, new AssetAnimationKey.BattleEffect.Status(type, isLarge));
            }
        }
    }

    private void AddUnitSounds(List<AssetPreloadItem> items, UnitDefinitionId definitionId)
    {
        for (int variant = 0; variant < AssetSoundKey.BattleUnitAttack.VariantCount; variant++)
        {
            AddSound(items, new AssetSoundKey.BattleUnitAttack(definitionId, variant));
        }

        for (int variant = 0; variant < AssetSoundKey.BattleUnitHit.VariantCount; variant++)
        {
            AddSound(items, new AssetSoundKey.BattleUnitHit(definitionId, variant));
        }

        for (int variant = 0; variant < AssetSoundKey.BattleUnitTouch.VariantCount; variant++)
        {
            AddSound(items, new AssetSoundKey.BattleUnitTouch(definitionId, variant));
        }

        for (int variant = 0; variant < AssetSoundKey.BattleUnitDamaged.VariantCount; variant++)
        {
            AddSound(items, new AssetSoundKey.BattleUnitDamaged(definitionId, variant));
        }
    }

    private void AddBattleEffectSounds(List<AssetPreloadItem> items)
    {
        AddSound(items, new AssetSoundKey.BattleEffect.Heal());
        AddSound(items, new AssetSoundKey.BattleEffect.Death());

        foreach (AssetSoundKey.BattleEffect.StatusType status in Enum.GetValues<AssetSoundKey.BattleEffect.StatusType>())
        {
            AddSound(items, new AssetSoundKey.BattleEffect.Status(status));
        }
    }

    private void AddCorpses(List<AssetPreloadItem> items)
    {
        foreach (bool isLarge in ResolveSizes())
        {
            for (int variant = 0; variant < AssetSpriteKey.BattleCorpse.VariantCount; variant++)
            {
                AddSprite(items, new AssetSpriteKey.BattleCorpse(isLarge, variant));
            }
        }
    }

    private void AddAnimation(List<AssetPreloadItem> items, AssetAnimationKey key)
    {
        if (keys.Add(key))
        {
            items.Add(new AssetPreloadItem.Animation(key));
        }
    }

    private void AddSprite(List<AssetPreloadItem> items, AssetSpriteKey key)
    {
        if (keys.Add(key))
        {
            items.Add(new AssetPreloadItem.Sprite(key));
        }
    }

    private void AddSound(List<AssetPreloadItem> items, AssetSoundKey key)
    {
        if (keys.Add(key))
        {
            items.Add(new AssetPreloadItem.Sound(key));
        }
    }

    private IReadOnlyList<bool> ResolveSizes()
    {
        List<bool> sizes = [];

        foreach (BattleRenderState.Unit unit in state.Units)
        {
            if (!sizes.Contains(unit.Footprint.IsLarge))
            {
                sizes.Add(unit.Footprint.IsLarge);
            }
        }

        return sizes;
    }

    private IReadOnlyList<UnitForm> ResolvePresentForms()
    {
        List<UnitForm> forms = [];

        foreach (BattleRenderState.Unit unit in state.Units)
        {
            UnitForm form = new(unit.DefinitionId, unit.Footprint.Side);

            if (!forms.Contains(form))
            {
                forms.Add(form);
            }
        }

        return forms;
    }

    private IReadOnlyList<UnitForm> ResolveDerivedForms(IReadOnlyList<UnitForm> present)
    {
        List<UnitForm> derived = [];
        HashSet<UnitForm> visited = [.. present];
        Queue<UnitForm> pending = new(present);

        while (pending.Count > 0)
        {
            UnitForm form = pending.Dequeue();

            foreach (UnitForm next in ResolveTransformedForms(form, present))
            {
                if (visited.Add(next))
                {
                    derived.Add(next);
                    pending.Enqueue(next);
                }
            }
        }

        return derived;
    }

    private IReadOnlyList<UnitForm> ResolveTransformedForms(UnitForm form, IReadOnlyList<UnitForm> present)
    {
        List<UnitForm> forms = [];

        foreach (UnitAttackDefinition attack in ResolveAttacks(definitions.GetUnit(form.DefinitionId)))
        {
            foreach (UnitDefinitionId summonId in attack.SummonUnitIds)
            {
                forms.Add(form with { DefinitionId = summonId });
            }

            if (attack.Type is UnitAttackType.Doppelganger)
            {
                foreach (UnitForm target in present)
                {
                    forms.Add(new UnitForm(target.DefinitionId, form.Facing));
                }
            }
            else if (attack.Type is UnitAttackType.TransformSelf or UnitAttackType.TransformEnemy)
            {
                BattleSide facing;

                if (attack.Type is UnitAttackType.TransformEnemy)
                {
                    facing = form.Facing.GetOpposite();
                }
                else
                {
                    facing = form.Facing;
                }

                if (attack.SmallTransformUnitId is { } smallId)
                {
                    forms.Add(new UnitForm(smallId, facing));
                }

                if (attack.LargeTransformUnitId is { } largeId)
                {
                    forms.Add(new UnitForm(largeId, facing));
                }
            }
        }

        return forms;
    }

    private IReadOnlyList<UnitForm> ResolveUpgradeForms(IReadOnlyList<UnitForm> present)
    {
        List<UnitForm> forms = [];

        foreach (UnitForm form in present)
        {
            foreach (UnitUpgradeDefinition upgrade in definitions.GetUnitUpgrades(form.DefinitionId))
            {
                UnitForm upgraded = form with { DefinitionId = upgrade.ToUnitId };

                if (!forms.Contains(upgraded))
                {
                    forms.Add(upgraded);
                }
            }
        }

        return forms;
    }

    private IReadOnlySet<AssetAnimationKey.BattleEffect.StatusType> ResolveStatusTypes(
        IReadOnlyList<UnitForm> present,
        IReadOnlyList<UnitForm> derived)
    {
        HashSet<AssetAnimationKey.BattleEffect.StatusType> types = [];

        foreach (UnitAttackType attackType in ResolveAttackTypes(present, derived))
        {
            foreach (AssetAnimationKey.BattleEffect.StatusType type in MapStatusTypes(attackType))
            {
                types.Add(type);
            }
        }

        return types;
    }

    private IReadOnlyList<UnitAttackType> ResolveAttackTypes(
        IReadOnlyList<UnitForm> present,
        IReadOnlyList<UnitForm> derived)
    {
        List<UnitAttackType> types = [];

        foreach (UnitForm form in present.Concat(derived))
        {
            foreach (UnitAttackDefinition attack in ResolveAttacks(definitions.GetUnit(form.DefinitionId)))
            {
                types.Add(attack.Type);
            }
        }

        foreach (UnitAttackDefinitionId attackId in ResolveEquipmentAttackIds())
        {
            UnitAttackDefinition attack = definitions.GetAttack(attackId);

            types.Add(attack.Type);
        }

        foreach (BattleItem item in battle.Items)
        {
            ItemDefinition definition = definitions.GetItem(item.ItemId);

            if (definition.Effect?.Accept(ItemEffectVisitor.Instance, definitions) is { } itemType)
            {
                types.Add(itemType);
            }
        }

        return types;
    }

    private IReadOnlyList<UnitAttackDefinitionId> ResolveEquipmentAttackIds()
    {
        List<UnitAttackDefinitionId> attackIds = [];

        AddGrid(battle.AttackerGrid);
        AddGrid(battle.DefenderGrid);

        return attackIds;

        void AddGrid(UnitGrid<BattleUnit> grid)
        {
            foreach ((BattleUnit unit, GridPosition _) in grid.GetUnits())
            {
                attackIds.AddRange(unit.EquipmentAttackIds);
            }
        }
    }

    private IReadOnlyList<UnitAttackDefinition> ResolveAttacks(UnitDefinition definition)
    {
        List<UnitAttackDefinition> attacks = [];
        HashSet<UnitAttackDefinitionId> visited = [];
        Queue<UnitAttackDefinition> queue = new();

        queue.Enqueue(definition.Attack);

        if (definition.SecondaryAttack is not null)
        {
            queue.Enqueue(definition.SecondaryAttack);
        }

        while (queue.Count > 0)
        {
            UnitAttackDefinition attack = queue.Dequeue();

            if (visited.Add(attack.Id))
            {
                attacks.Add(attack);

                if (attack.AlternativeAttackId is { } alternativeId)
                {
                    queue.Enqueue(definitions.GetAttack(alternativeId));
                }
            }
        }

        return attacks;
    }

    private static IReadOnlyList<AssetAnimationKey.BattleEffect.StatusType> MapStatusTypes(UnitAttackType attackType)
    {
        return attackType switch
        {
            UnitAttackType.Poison => [AssetAnimationKey.BattleEffect.StatusType.Poison],
            UnitAttackType.Frostbite => [AssetAnimationKey.BattleEffect.StatusType.Frostbite],
            UnitAttackType.Blister => [AssetAnimationKey.BattleEffect.StatusType.Blister],
            UnitAttackType.Revive => [AssetAnimationKey.BattleEffect.StatusType.Revive],
            UnitAttackType.ReduceLevel =>
            [
                AssetAnimationKey.BattleEffect.StatusType.ReduceLevel,
                AssetAnimationKey.BattleEffect.StatusType.RestoreLevel
            ],
            UnitAttackType.Summon => [AssetAnimationKey.BattleEffect.StatusType.Unsummon],
            UnitAttackType.TransformSelf =>
            [
                AssetAnimationKey.BattleEffect.StatusType.Transform,
                AssetAnimationKey.BattleEffect.StatusType.Untransform
            ],
            UnitAttackType.TransformEnemy =>
            [
                AssetAnimationKey.BattleEffect.StatusType.Transform,
                AssetAnimationKey.BattleEffect.StatusType.Untransform
            ],
            UnitAttackType.Doppelganger =>
            [
                AssetAnimationKey.BattleEffect.StatusType.Doppelganger,
                AssetAnimationKey.BattleEffect.StatusType.Undoppelganger
            ],
            UnitAttackType.Damage => [],
            UnitAttackType.DrainLife => [],
            UnitAttackType.DrainLifeOverflow => [],
            UnitAttackType.Paralyze => [],
            UnitAttackType.Heal => [],
            UnitAttackType.Fear => [],
            UnitAttackType.IncreaseDamage => [],
            UnitAttackType.Petrify => [],
            UnitAttackType.ReduceDamage => [],
            UnitAttackType.ReduceInitiative => [],
            UnitAttackType.Cure => [],
            UnitAttackType.GiveAdditionalAttack => [],
            UnitAttackType.GiveProtection => [],
            UnitAttackType.ReduceArmor => []
        };
    }
}
