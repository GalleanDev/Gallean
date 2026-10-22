using CommunityToolkit.Mvvm.ComponentModel;
using Gallean.Engine;

namespace Gallean.Client;

internal sealed partial class RaceSelectItemViewModel : ObservableObject
{
    private readonly RaceSelectScreenViewModel screen;

    public RaceSelectItemViewModel(RaceSelectScreenViewModel screen, ScenarioRace scenarioRace, string name, PlayerType playerType)
    {
        this.screen = screen;

        ScenarioRace = scenarioRace;
        Name = name;
        PlayerType = playerType;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsHuman))]
    public partial PlayerType PlayerType { get; set; }

    public ScenarioRace ScenarioRace { get; }
    public string Name { get; }

    public IReadOnlyList<PlayerType> PlayerTypes { get; } = Enum.GetValues<PlayerType>();
    public WorldRace Race => ScenarioRace.Race;
    public bool IsHuman => PlayerType == PlayerType.Human;

    partial void OnPlayerTypeChanged(PlayerType value)
    {
        screen.OnPlayerTypeChanged();
    }
}
