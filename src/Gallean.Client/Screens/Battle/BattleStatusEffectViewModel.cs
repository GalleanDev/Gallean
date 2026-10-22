using Avalonia.Media;
using static Gallean.Client.BattleUnitState;

namespace Gallean.Client;

internal sealed record BattleStatusEffectViewModel(StatusEffect Effect) : UnitStatusViewModel
{
    public override string Name => Effect.Type switch
    {
        StatusEffectType.Defend => Tr("Defending"),
        StatusEffectType.Paralyze => Tr("Paralyzed"),
        StatusEffectType.Petrify => Tr("Petrified"),
        StatusEffectType.Poison => Tr("Poisoned"),
        StatusEffectType.Frostbite => Tr("Frostbitten"),
        StatusEffectType.Blister => Tr("Blistered"),
        StatusEffectType.Retreat => Tr("Retreating"),
        StatusEffectType.Retreated => Tr("Retreated"),
        StatusEffectType.Fortification => Tr("Fortified"),
        StatusEffectType.IncreaseDamage => Tr("Damage boosted"),
        StatusEffectType.ReduceDamage => Tr("Damage reduced"),
        StatusEffectType.ReduceInitiative => Tr("Initiative reduced"),
        StatusEffectType.ShatterArmor => Tr("Armor shattered"),
        StatusEffectType.Cure => Tr("Cured"),
        StatusEffectType.Transform => Tr("Transformed"),
        StatusEffectType.TransformSelf => Tr("Transformed"),
        StatusEffectType.Doppelganger => Tr("Doppelganger"),
        StatusEffectType.LevelDrain => Tr("Level drained")
    };

    public override TextSpans Description => Effect.Type switch
    {
        StatusEffectType.Defend => TrSpans("Armor is increased until this unit's next turn."),
        StatusEffectType.Paralyze => TrSpans("Cannot act while the paralysis lasts."),
        StatusEffectType.Petrify => TrSpans("Turned to stone and cannot act while the petrification lasts."),
        StatusEffectType.Poison => TrSpans("Takes poison damage at the start of each turn."),
        StatusEffectType.Frostbite => TrSpans("Takes frost damage at the start of each turn."),
        StatusEffectType.Blister => TrSpans("Takes fire damage at the start of each turn."),
        StatusEffectType.Retreat => TrSpans("Leaves the battle on its next turn."),
        StatusEffectType.Retreated => TrSpans("Has fled from battle."),
        StatusEffectType.Fortification => TrSpans("City walls increase this unit's armor."),
        StatusEffectType.IncreaseDamage => TrSpans("Deals more damage while the boost lasts."),
        StatusEffectType.ReduceDamage => TrSpans("Deals less damage while the weakness lasts."),
        StatusEffectType.ReduceInitiative => TrSpans("Acts later in the round while the slowness lasts."),
        StatusEffectType.ShatterArmor => TrSpans("Armor is reduced for the rest of the battle."),
        StatusEffectType.Cure => TrSpans("Has been cured."),
        StatusEffectType.Transform => TrSpans("Turned into another creature by an enemy."),
        StatusEffectType.TransformSelf => TrSpans("Has taken on another form."),
        StatusEffectType.Doppelganger => TrSpans("Has copied the form of another unit."),
        StatusEffectType.LevelDrain => TrSpans("Level reduced by an enemy.")
    };

    public override UnitStatusCategory Category => Effect.Type switch
    {
        StatusEffectType.Defend => UnitStatusCategory.Buff,
        StatusEffectType.Paralyze => UnitStatusCategory.Disable,
        StatusEffectType.Petrify => UnitStatusCategory.Disable,
        StatusEffectType.Poison => UnitStatusCategory.Affliction,
        StatusEffectType.Frostbite => UnitStatusCategory.Affliction,
        StatusEffectType.Blister => UnitStatusCategory.Affliction,
        StatusEffectType.Retreat => UnitStatusCategory.Neutral,
        StatusEffectType.Retreated => UnitStatusCategory.Neutral,
        StatusEffectType.Fortification => UnitStatusCategory.Buff,
        StatusEffectType.IncreaseDamage => UnitStatusCategory.Buff,
        StatusEffectType.ReduceDamage => UnitStatusCategory.Debuff,
        StatusEffectType.ReduceInitiative => UnitStatusCategory.Debuff,
        StatusEffectType.ShatterArmor => UnitStatusCategory.Debuff,
        StatusEffectType.Cure => UnitStatusCategory.Neutral,
        StatusEffectType.Transform => UnitStatusCategory.Debuff,
        StatusEffectType.TransformSelf => UnitStatusCategory.Neutral,
        StatusEffectType.Doppelganger => UnitStatusCategory.Neutral,
        StatusEffectType.LevelDrain => UnitStatusCategory.Debuff
    };

    public override IconMoniker Icon => Effect.Type switch
    {
        StatusEffectType.Defend => IconMoniker.Defend,
        StatusEffectType.Paralyze => IconMoniker.Paralyze,
        StatusEffectType.Petrify => IconMoniker.Petrify,
        StatusEffectType.Poison => IconMoniker.Poison,
        StatusEffectType.Frostbite => IconMoniker.Frostbite,
        StatusEffectType.Blister => IconMoniker.Blister,
        StatusEffectType.Retreat => IconMoniker.Retreat,
        StatusEffectType.Retreated => IconMoniker.Retreat,
        StatusEffectType.Fortification => IconMoniker.Fortification,
        StatusEffectType.IncreaseDamage => IconMoniker.IncreaseDamage,
        StatusEffectType.ReduceDamage => IconMoniker.ReduceDamage,
        StatusEffectType.ReduceInitiative => IconMoniker.ReduceInitiative,
        StatusEffectType.ShatterArmor => IconMoniker.ShatterArmor,
        StatusEffectType.Cure => IconMoniker.Cure,
        StatusEffectType.Transform => IconMoniker.Transform,
        StatusEffectType.TransformSelf => IconMoniker.Transform,
        StatusEffectType.Doppelganger => IconMoniker.Transform,
        StatusEffectType.LevelDrain => IconMoniker.ReduceLevel
    };

    public override Color Color => Effect.Type switch
    {
        StatusEffectType.Defend => Theme.Battle.StatusEffect.DefendColor,
        StatusEffectType.Paralyze => Theme.Battle.StatusEffect.ParalyzeColor,
        StatusEffectType.Petrify => Theme.Battle.StatusEffect.PetrifyColor,
        StatusEffectType.Poison => Theme.Battle.StatusEffect.PoisonColor,
        StatusEffectType.Frostbite => Theme.Battle.StatusEffect.FrostbiteColor,
        StatusEffectType.Blister => Theme.Battle.StatusEffect.BlisterColor,
        StatusEffectType.Retreat => Theme.Battle.StatusEffect.RetreatColor,
        StatusEffectType.Retreated => Theme.Battle.StatusEffect.RetreatColor,
        StatusEffectType.Fortification => Theme.Battle.StatusEffect.FortificationColor,
        StatusEffectType.IncreaseDamage => Theme.Battle.StatusEffect.IncreaseDamageColor,
        StatusEffectType.ReduceDamage => Theme.Battle.StatusEffect.ReduceDamageColor,
        StatusEffectType.ReduceInitiative => Theme.Battle.StatusEffect.ReduceInitiativeColor,
        StatusEffectType.ShatterArmor => Theme.Battle.StatusEffect.ShatterArmorColor,
        StatusEffectType.Cure => Theme.Battle.StatusEffect.CureColor,
        StatusEffectType.Transform => Theme.Battle.StatusEffect.TransformColor,
        StatusEffectType.TransformSelf => Theme.Battle.StatusEffect.TransformColor,
        StatusEffectType.Doppelganger => Theme.Battle.StatusEffect.TransformColor,
        StatusEffectType.LevelDrain => Theme.Battle.StatusEffect.ReduceLevelColor
    };

    public override Color? Overlay => Effect.Type switch
    {
        StatusEffectType.Petrify => Theme.Battle.StatusEffect.PetrifyOverlayColor,
        StatusEffectType.Paralyze => Theme.Battle.StatusEffect.ParalyzeOverlayColor,
        StatusEffectType.Poison => Theme.Battle.StatusEffect.PoisonOverlayColor,
        StatusEffectType.Frostbite => Theme.Battle.StatusEffect.FrostbiteOverlayColor,
        StatusEffectType.Blister => Theme.Battle.StatusEffect.BlisterOverlayColor,
        _ => null
    };
}
