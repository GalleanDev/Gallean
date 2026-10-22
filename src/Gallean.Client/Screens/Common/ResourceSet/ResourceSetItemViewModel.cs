namespace Gallean.Client;

internal sealed record ResourceSetItemViewModel(WorldResourceType Type, int Value, int PreviousValue)
{
    private const int ValueChangeFrames = 15;

    public IReadOnlyList<AnimationProperty> ValueAnimation
    {
        get
        {
            if (Value != PreviousValue)
            {
                return
                [
                    new AnimationProperty(
                        new ResourceSetValueAnimationTarget(Type),
                        TimeSpan.Zero,
                        AnimationRate.Sprites.GetDuration(ValueChangeFrames),
                        PreviousValue,
                        Value,
                        AnimationEasing.Decelerate)
                ];
            }
            else
            {
                return [];
            }
        }
    }

    public string Name => Type switch
    {
        WorldResourceType.Gold => Tr("Gold"),
        WorldResourceType.LifeMana => Tr("Life mana"),
        WorldResourceType.DeathMana => Tr("Death mana"),
        WorldResourceType.RuneMana => Tr("Rune mana"),
        WorldResourceType.InfernalMana => Tr("Infernal mana"),
        WorldResourceType.GroveMana => Tr("Grove mana")
    };

    public IconMoniker Icon => Type switch
    {
        WorldResourceType.Gold => IconMoniker.Gold,
        WorldResourceType.LifeMana => IconMoniker.ManaLife,
        WorldResourceType.DeathMana => IconMoniker.ManaDeath,
        WorldResourceType.RuneMana => IconMoniker.ManaRune,
        WorldResourceType.InfernalMana => IconMoniker.ManaInfernal,
        WorldResourceType.GroveMana => IconMoniker.ManaGrove
    };
}
