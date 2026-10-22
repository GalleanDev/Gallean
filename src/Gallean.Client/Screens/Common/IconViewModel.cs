namespace Gallean.Client;

internal abstract record IconViewModel
{
    public sealed record Item(AssetSpriteKey.ItemIcon Key) : IconViewModel;

    public sealed record Unit(AssetSpriteKey.UnitPortrait Key) : IconViewModel;

    public sealed record Spell(AssetSpriteKey.SpellIcon Key, Spell.BackgroundType Background) : IconViewModel
    {
        public enum BackgroundType
        {
            Light,
            Dark
        }
    }

    private IconViewModel()
    {
    }
}
