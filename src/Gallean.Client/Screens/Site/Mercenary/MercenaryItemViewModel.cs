using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Gallean.Client;

internal sealed partial class MercenaryItemViewModel : ObservableObject, IDragSource, IDetailsSource
{
    private readonly bool isLarge;

    public MercenaryItemViewModel(
        MercenaryTabViewModel owner,
        UnitDefinitionId unitDefinitionId,
        int level,
        bool isLarge,
        string name,
        string description,
        string levelText,
        OfferStatusViewModel status,
        UnitDetailsViewModel details)
    {
        this.isLarge = isLarge;

        UnitDefinitionId = unitDefinitionId;
        Level = level;
        PortraitKey = new AssetSpriteKey.UnitPortrait(unitDefinitionId);
        Name = name;
        Description = description;
        LevelText = levelText;
        Status = status;
        Details = details;
        SelectCommand = new RelayCommand(() => owner.SelectMercenary(this));
    }

    public UnitDefinitionId UnitDefinitionId { get; }
    public int Level { get; }
    public AssetSpriteKey.UnitPortrait PortraitKey { get; }
    public string Name { get; }
    public string Description { get; }
    public string LevelText { get; }
    public OfferStatusViewModel Status { get; }
    public UnitDetailsViewModel Details { get; }

    public IRelayCommand SelectCommand { get; }

    [ObservableProperty]
    public partial bool IsSelected { get; set; }

    public DragPreview? DragPreview
    {
        get
        {
            AssetSpriteKey.UnitFace faceKey = new(UnitDefinitionId);

            if (isLarge)
            {
                return new DragPreview(faceKey, DragPreviewType.LargeUnit);
            }
            else
            {
                return new DragPreview(faceKey, DragPreviewType.SmallUnit);
            }
        }
    }

    public MercenaryReference Reference => new(UnitDefinitionId, Level);

    IDragSourceReference IDragSource.DragReference => Reference;

    object? IDetailsSource.DetailsContent => Details;
}
