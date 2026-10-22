using CommunityToolkit.Mvvm.Input;

namespace Gallean.Client;

internal sealed record SiteOfferViewModel(
    IconViewModel Icon,
    object Details,
    string Title,
    TextSpans Subtitle,
    string Description,
    string ActionText,
    OfferStatusViewModel Status,
    IAsyncRelayCommand ActCommand) : IDetailsSource
{
    object? IDetailsSource.DetailsContent => Details;
}
