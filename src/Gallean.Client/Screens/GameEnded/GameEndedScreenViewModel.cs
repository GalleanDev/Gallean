using System.Collections.Immutable;

namespace Gallean.Client;

internal sealed class GameEndedScreenViewModel(
    bool isVictory,
    string scenarioName,
    string debriefing,
    ScenarioTextFormatter formatter,
    MenuBackgroundViewModel? background,
    ScreenMusic music,
    GameViewModel game,
    bool canContinueCampaign) : AbstractScreenViewModel
{
    public GameViewModel Game { get; } = game;
    public bool IsVictory { get; } = isVictory;
    public bool CanContinueCampaign { get; } = canContinueCampaign;
    public string Title => IsVictory ? Tr("Victory") : Tr("Defeat");
    public string ScenarioName { get; } = scenarioName;
    public ImmutableArray<ScenarioTextSpan> Debriefing { get; } = formatter.Format(ScenarioFormatter.FormatBriefing(debriefing));
    public MenuBackgroundViewModel? Background { get; } = background;

    public override ScreenMusic Music { get; } = music;
}
