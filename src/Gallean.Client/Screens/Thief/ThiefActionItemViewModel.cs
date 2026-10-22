using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Gallean.Client;

internal sealed class ThiefActionItemViewModel : ObservableObject
{
    public ThiefActionItemViewModel(
        ThiefActionScreenViewModel owner,
        WorldThiefActionType type,
        string name,
        string description,
        TextSpans chanceText,
        string? unavailableReason)
    {
        Type = type;
        Icon = ThiefActionIconResolver.Resolve(type);
        Name = name;
        Description = description;
        ChanceText = chanceText;
        Status = new OfferStatusViewModel(unavailableReason);
        SelectCommand = new AsyncRelayCommand(() => owner.SelectActionAsync(this));
    }

    public WorldThiefActionType Type { get; }
    public IconMoniker Icon { get; }
    public string Name { get; }
    public string Description { get; }
    public TextSpans ChanceText { get; }
    public OfferStatusViewModel Status { get; }

    public IAsyncRelayCommand SelectCommand { get; }
}
