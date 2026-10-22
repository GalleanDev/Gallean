namespace Gallean.Client;

internal sealed class HotseatTurnScreenViewModel(
    string lordName,
    string raceName,
    AssetSpriteKey.LordFace portraitKey) : AbstractScreenViewModel
{
    public string LordName { get; } = lordName;
    public string RaceTitle { get; } = Tr("Lord of the {0}", raceName);
    public AssetSpriteKey.LordFace PortraitKey { get; } = portraitKey;

    public override AssetSoundKey OpenSound => new AssetSoundKey.InterfaceOpen();

    protected override bool TryClose()
    {
        CloseSelf();

        return true;
    }
}
