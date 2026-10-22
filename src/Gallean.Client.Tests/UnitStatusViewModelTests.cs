namespace Gallean.Client.Tests;

public sealed class UnitStatusViewModelTests
{
    [Test]
    public void CreateAllTest()
    {
        UnitModifierViewModel strength = new("+30% damage", TrSpans("Turns left: {0:*}", 3), UnitModifierType.Boost);
        UnitModifierViewModel swiftness = new("+15% initiative", TrSpans("Turns left: {0:*}", 2), UnitModifierType.Boost);
        UnitModifierViewModel protection = new("Ward against Fire", TrSpans("Until the caster's next turn"), UnitModifierType.Boost);
        UnitModifierViewModel curse = new("-20% hit points", TrSpans("Turns left: {0:*}", 1), UnitModifierType.Weaken);

        IReadOnlyList<UnitStatusViewModel> statuses = UnitStatusViewModel.CreateAll(
            [
                new BattleUnitState.StatusEffect(BattleUnitState.StatusEffectType.Defend),
                new BattleUnitState.StatusEffect(BattleUnitState.StatusEffectType.Poison)
            ],
            [strength, swiftness, curse, protection]);

        Assert.That(statuses, Is.EqualTo(new UnitStatusViewModel[]
        {
            new BattleStatusEffectViewModel(new BattleUnitState.StatusEffect(BattleUnitState.StatusEffectType.Poison)),
            new UnitModifierStatusViewModel(UnitModifierType.Weaken, [new UnitModifierViewModel("-20% hit points", TrSpans("Turns left: {0:*}", 1), UnitModifierType.Weaken)]),
            new BattleStatusEffectViewModel(new BattleUnitState.StatusEffect(BattleUnitState.StatusEffectType.Defend)),
            new UnitModifierStatusViewModel(UnitModifierType.Boost,
            [
                new UnitModifierViewModel("+30% damage", TrSpans("Turns left: {0:*}", 3), UnitModifierType.Boost),
                new UnitModifierViewModel("+15% initiative", TrSpans("Turns left: {0:*}", 2), UnitModifierType.Boost),
                new UnitModifierViewModel("Ward against Fire", TrSpans("Until the caster's next turn"), UnitModifierType.Boost)
            ])
        }));
    }

    [Test]
    public void ModifierStatusTest()
    {
        UnitModifierStatusViewModel status = new(UnitModifierType.Boost,
        [
            new UnitModifierViewModel("+30% damage", TrSpans("Turns left: {0:*}", 3), UnitModifierType.Boost),
            new UnitModifierViewModel("+15% initiative", null, UnitModifierType.Boost),
            new UnitModifierViewModel("Ward against Fire", TrSpans("Until the caster's next turn"), UnitModifierType.Boost)
        ]);

        Assert.That(status.Name, Is.EqualTo("Boosted"));
        Assert.That(status.Description, Is.EqualTo(new TextSpans(
        [
            new TextSpan("+30% damage", TextStyle.None),
            new TextSpan(" – ", TextStyle.None),
            new TextSpan("Turns left: ", TextStyle.None),
            new TextSpan("3", TextStyle.Highlight),
            new TextSpan("\n", TextStyle.None),
            new TextSpan("+15% initiative", TextStyle.None),
            new TextSpan("\n", TextStyle.None),
            new TextSpan("Ward against Fire", TextStyle.None),
            new TextSpan(" – ", TextStyle.None),
            new TextSpan("Until the caster's next turn", TextStyle.None)
        ])));
        Assert.That(status.Count, Is.EqualTo(3));
        Assert.That(status.Category, Is.EqualTo(UnitStatusCategory.Buff));
        Assert.That(status.Icon, Is.EqualTo(IconMoniker.TemporaryModifier));
        Assert.That(status.Color, Is.EqualTo(Theme.Battle.StatusEffect.BoostModifierColor));
    }
}
