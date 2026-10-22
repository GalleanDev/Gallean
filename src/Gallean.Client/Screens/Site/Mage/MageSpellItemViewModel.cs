using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Gallean.Client;

internal sealed partial class MageSpellItemViewModel : ObservableObject, IDetailsSource
{
    public MageSpellItemViewModel(
        MageTabViewModel owner,
        WorldSpellDefinitionId spellId,
        string name,
        string description,
        string levelText,
        OfferStatusViewModel status,
        SpellDetailsViewModel details)
    {
        SpellId = spellId;
        Name = name;
        Description = description;
        LevelText = levelText;
        Status = status;
        Details = details;
        SelectCommand = new RelayCommand(() => owner.SelectSpell(this));
    }

    public WorldSpellDefinitionId SpellId { get; }
    public string Name { get; }
    public string Description { get; }
    public string LevelText { get; }
    public OfferStatusViewModel Status { get; }
    public SpellDetailsViewModel Details { get; }

    public IRelayCommand SelectCommand { get; }

    [ObservableProperty]
    public partial bool IsSelected { get; set; }

    object? IDetailsSource.DetailsContent => Details;
}
