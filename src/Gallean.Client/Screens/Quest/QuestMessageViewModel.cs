namespace Gallean.Client;

internal sealed record QuestMessageViewModel(ScenarioEventPopupMessage Message)
{
    public bool IsNarration => Message.PortraitKey is null;
    public bool HasLeftPortrait => Message.PortraitKey is not null && Message.IsPortraitLeft;
    public bool HasRightPortrait => Message.PortraitKey is not null && !Message.IsPortraitLeft;
    public bool HasSpeaker => Message.PortraitName.Length > 0;
}
