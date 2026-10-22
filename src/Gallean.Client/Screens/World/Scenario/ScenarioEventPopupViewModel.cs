using System.Collections.Immutable;
using System.Collections.ObjectModel;

namespace Gallean.Client;

internal sealed class ScenarioEventPopupViewModel : AbstractLiveBackgroundScreenViewModel
{
    private readonly ImmutableArray<ScenarioEventPopupMessage> messages;

    public ScenarioEventPopupViewModel(ImmutableArray<ScenarioEventPopupMessage> messages)
    {
        this.messages = messages;

        LeftPortrait = new ScenarioEventPopupPortraitViewModel(FindPortraitKey(messages, isPortraitLeft: true));
        RightPortrait = new ScenarioEventPopupPortraitViewModel(FindPortraitKey(messages, isPortraitLeft: false));

        Reveal(messages[0]);
    }

    public ObservableCollection<ScenarioEventPopupMessageViewModel> Messages { get; } = [];
    public ScenarioEventPopupPortraitViewModel LeftPortrait { get; }
    public ScenarioEventPopupPortraitViewModel RightPortrait { get; }

    public override TimeSpan RevealDuration => TimeSpan.FromMilliseconds(250);
    public override AssetSoundKey OpenSound => new AssetSoundKey.InterfaceOpen();

    protected override bool TryClose()
    {
        if (Messages.Count < messages.Length)
        {
            Reveal(messages[Messages.Count]);

            return false;
        }
        else
        {
            CloseSelf();

            return true;
        }
    }

    private static AssetSpriteKey? FindPortraitKey(ImmutableArray<ScenarioEventPopupMessage> messages, bool isPortraitLeft)
    {
        foreach (ScenarioEventPopupMessage message in messages)
        {
            if (message.PortraitKey is not null && message.IsPortraitLeft == isPortraitLeft)
            {
                return message.PortraitKey;
            }
        }

        return null;
    }

    private void Reveal(ScenarioEventPopupMessage message)
    {
        Messages.Add(new ScenarioEventPopupMessageViewModel(message));

        if (message.PortraitKey is { } portraitKey)
        {
            ScenarioEventPopupPortraitViewModel portraitViewModel = message.IsPortraitLeft ? LeftPortrait : RightPortrait;
            portraitViewModel.PortraitKey = portraitKey;

            LeftPortrait.IsActive = message.IsPortraitLeft;
            RightPortrait.IsActive = !message.IsPortraitLeft;
        }
        else
        {
            LeftPortrait.IsActive = false;
            RightPortrait.IsActive = false;
        }
    }
}
