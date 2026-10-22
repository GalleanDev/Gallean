using CommunityToolkit.Mvvm.ComponentModel;

namespace Gallean.Client;

internal sealed partial class SpellbookItemViewModel : ObservableObject, IDetailsSource
{
    public SpellbookItemViewModel(
        WorldSpellDefinitionId spellId,
        SpellbookItemType type,
        int level,
        bool isTargeted,
        OfferStatusViewModel status,
        SpellDetailsViewModel details)
    {
        SpellId = spellId;
        Type = type;
        Level = level;
        IsTargeted = isTargeted;
        Status = status;
        Details = details;
    }

    public WorldSpellDefinitionId SpellId { get; }
    public SpellbookItemType Type { get; }
    public int Level { get; }
    public bool IsTargeted { get; }
    public OfferStatusViewModel Status { get; }
    public SpellDetailsViewModel Details { get; }

    public string? ActionText => Type switch
    {
        SpellbookItemType.Known => Tr("Cast"),
        SpellbookItemType.Researchable => Tr("Research"),
        SpellbookItemType.Locked => null
    };

    [ObservableProperty]
    public partial bool IsSelected { get; set; }

    object? IDetailsSource.DetailsContent => Details;
}
