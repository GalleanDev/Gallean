using CommunityToolkit.Mvvm.ComponentModel;

namespace Gallean.Client;

internal sealed partial class ScenarioEventPopupPortraitViewModel : ObservableObject
{
    public ScenarioEventPopupPortraitViewModel(AssetSpriteKey? portraitKey)
    {
        PortraitKey = portraitKey;
    }

    [ObservableProperty]
    public partial AssetSpriteKey? PortraitKey { get; set; }

    [ObservableProperty]
    public partial bool IsActive { get; set; }
}
