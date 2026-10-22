namespace Gallean.Client.Tests;

public sealed class BattleStatusEffectViewModelTests
{
    [Test]
    public void PresentationTest()
    {
        BattleStatusEffectViewModel effect = new(new BattleUnitState.StatusEffect(BattleUnitState.StatusEffectType.Poison));

        Assert.That(effect.Name, Is.EqualTo("Poisoned"));
        Assert.That(effect.Description, Is.EqualTo(TextSpans.FromText("Takes poison damage at the start of each turn.")));
        Assert.That(effect.Category, Is.EqualTo(UnitStatusCategory.Affliction));
        Assert.That(effect.Icon, Is.EqualTo(IconMoniker.Poison));
        Assert.That(effect.Color, Is.EqualTo(Theme.Battle.StatusEffect.PoisonColor));
        Assert.That(effect.Overlay, Is.EqualTo(Theme.Battle.StatusEffect.PoisonOverlayColor));
    }

    [Test]
    public void CuredTest()
    {
        BattleStatusEffectViewModel effect = new(new BattleUnitState.StatusEffect(BattleUnitState.StatusEffectType.Cure));

        Assert.That(effect.Category, Is.EqualTo(UnitStatusCategory.Neutral));
    }

    [Test]
    public void PetrifiedTest()
    {
        BattleStatusEffectViewModel effect = new(new BattleUnitState.StatusEffect(BattleUnitState.StatusEffectType.Petrify));

        Assert.That(effect.Overlay, Is.EqualTo(Theme.Battle.StatusEffect.PetrifyOverlayColor));
    }

    [Test]
    public void FrostbittenTest()
    {
        BattleStatusEffectViewModel effect = new(new BattleUnitState.StatusEffect(BattleUnitState.StatusEffectType.Frostbite));

        Assert.That(effect.Overlay, Is.EqualTo(Theme.Battle.StatusEffect.FrostbiteOverlayColor));
    }

    [Test]
    public void BlisteredTest()
    {
        BattleStatusEffectViewModel effect = new(new BattleUnitState.StatusEffect(BattleUnitState.StatusEffectType.Blister));

        Assert.That(effect.Overlay, Is.EqualTo(Theme.Battle.StatusEffect.BlisterOverlayColor));
    }

    [Test]
    public void DefendedTest()
    {
        BattleStatusEffectViewModel effect = new(new BattleUnitState.StatusEffect(BattleUnitState.StatusEffectType.Defend));

        Assert.That(effect.Overlay, Is.Null);
    }
}
