using CommunityToolkit.Mvvm.Input;
using Gallean.Engine;

namespace Gallean.Client;

internal sealed class ThiefStealItemViewModel
{
    public ThiefStealItemViewModel(
        ThiefStealScreenViewModel owner,
        WorldThiefAction action,
        IconViewModel icon,
        string name,
        string description,
        string? unavailableReason)
    {
        Action = action;
        Icon = icon;
        Name = name;
        Description = description;
        Status = new OfferStatusViewModel(unavailableReason);
        SelectCommand = new AsyncRelayCommand(() => owner.StealAsync(this));
    }

    public WorldThiefAction Action { get; }
    public IconViewModel Icon { get; }
    public string Name { get; }
    public string Description { get; }
    public OfferStatusViewModel Status { get; }

    public IAsyncRelayCommand SelectCommand { get; }
}
