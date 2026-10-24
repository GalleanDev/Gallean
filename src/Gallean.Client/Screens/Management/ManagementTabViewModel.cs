using CommunityToolkit.Mvvm.ComponentModel;

namespace Gallean.Client;

internal abstract partial class ManagementTabViewModel(GameModel gameModel, GameViewModel gameViewModel)
    : AbstractGameScreenViewModel(gameModel, gameViewModel)
{
    public abstract string TabHeader { get; }
    public virtual AssetSpriteKey.SiteImage? SiteImageKey => null;
    public virtual ResourceSetProjection ResourceProjection => ResourceSetProjection.Gold;

    [ObservableProperty]
    public partial bool IsSelected { get; set; }

    public virtual bool TryCancel()
    {
        return false;
    }

    protected virtual void OnSelectionChanged(bool isSelected)
    {
    }

    partial void OnIsSelectedChanged(bool value)
    {
        OnSelectionChanged(value);
    }
}
