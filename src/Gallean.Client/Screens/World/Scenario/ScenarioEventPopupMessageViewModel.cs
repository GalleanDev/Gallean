namespace Gallean.Client;

internal sealed record ScenarioEventPopupMessageViewModel(ScenarioEventPopupMessage Message)
{
    private const int ShortNarrationLength = 80;

    public bool IsNarration => Message.PortraitKey is null;
    public bool IsShortNarration => IsNarration && Message.Spans.Sum(span => span.Text.Length) <= ShortNarrationLength;
}
