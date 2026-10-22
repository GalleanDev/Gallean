using Avalonia.Media;

namespace Gallean.Client;

internal abstract record UnitStatusViewModel
{
    public abstract string Name { get; }
    public abstract TextSpans Description { get; }
    public abstract UnitStatusCategory Category { get; }
    public abstract IconMoniker Icon { get; }
    public abstract Color Color { get; }
    public virtual Color? Overlay => null;
    public virtual int Count => 1;

    public static IReadOnlyList<UnitStatusViewModel> CreateAll(
        IEnumerable<BattleUnitState.StatusEffect> statusEffects,
        IReadOnlyList<UnitModifierViewModel> modifiers)
    {
        List<UnitStatusViewModel> statuses = [];

        foreach (BattleUnitState.StatusEffect effect in statusEffects)
        {
            statuses.Add(new BattleStatusEffectViewModel(effect));
        }

        statuses.AddRange(UnitModifierStatusViewModel.CreateAll(modifiers));

        return [.. statuses.OrderBy(status => status.Category)];
    }
}
