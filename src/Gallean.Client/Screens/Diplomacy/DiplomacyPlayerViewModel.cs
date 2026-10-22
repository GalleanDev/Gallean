using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Gallean.Engine;

namespace Gallean.Client;

internal sealed partial class DiplomacyPlayerViewModel : ObservableObject
{
    public DiplomacyPlayerViewModel(
        DiplomacyTabViewModel owner,
        WorldGetDiplomacyInfoCommand.Player info,
        string name,
        string raceName,
        AssetSpriteKey.LordFace portraitKey,
        AssetSpriteKey.LordFace largePortraitKey)
    {
        Info = info;
        Name = name;
        RaceName = raceName;
        PortraitKey = portraitKey;
        LargePortraitKey = largePortraitKey;
        RelationText = $"{FormatRelationLevel(info.Level)} ({info.Relation.Current})";
        StatusText = FormatStatusText(info.Relation);
        SelectCommand = new RelayCommand(() => owner.SelectPlayer(this));
    }

    public WorldGetDiplomacyInfoCommand.Player Info { get; }
    public WorldPlayerId PlayerId => Info.PlayerId;
    public string Name { get; }
    public string RaceName { get; }
    public AssetSpriteKey.LordFace PortraitKey { get; }
    public AssetSpriteKey.LordFace LargePortraitKey { get; }
    public string RelationText { get; }
    public string? StatusText { get; }

    [ObservableProperty]
    public partial bool IsSelected { get; set; }

    public IRelayCommand SelectCommand { get; }

    private static string FormatRelationLevel(WorldRelationLevel level)
    {
        return level switch
        {
            WorldRelationLevel.War => Tr("War"),
            WorldRelationLevel.Neutral => Tr("Neutral"),
            WorldRelationLevel.Peace => Tr("Peace")
        };
    }

    private static string? FormatStatusText(WorldRelation relation)
    {
        if (relation.Alliance)
        {
            return Tr("Allied");
        }
        else if (relation.PermanentWar)
        {
            return Tr("Permanent war");
        }
        else
        {
            return null;
        }
    }
}
