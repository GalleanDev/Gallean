using Gallean.Engine;
using Gallean.Tests.Common;

namespace Gallean.Client.Tests;

public sealed class ScenarioSelectScreenViewModelTests
{
    private static IReadOnlyList<string> FormatPlayers(WorldState state)
    {
        List<string> rows = [];

        foreach (WorldPlayer player in state.Players.Values)
        {
            string control = "ai";

            if (!player.IsAi)
            {
                control = "human";
            }

            rows.Add($"{player.Id} {player.Race} {player.LordId} {control}");
        }

        return rows;
    }

    private static IReadOnlyList<string> FormatRaces(IReadOnlyList<RaceSelectItemViewModel> races)
    {
        List<string> rows = [];

        foreach (RaceSelectItemViewModel race in races)
        {
            rows.Add($"{race.Race} {race.Name} {race.PlayerType}");
        }

        return rows;
    }

    [Test]
    public async Task ScenariosTest()
    {
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();

        await gameViewModel.ShowScenarioSelectCommand.ExecuteAsync(null);

        ScenarioSelectScreenViewModel viewModel = Assert.IsOfType<ScenarioSelectScreenViewModel>(gameViewModel.RootScreen);

        Assert.That(viewModel.Scenarios.Select(scenario => scenario.Id), Is.EqualTo([
            "S200SC0000",
            "S201SC0000",
            "S202SC0000",
            "S203SC0000",
            "S204SC0000",
            "S205SC0000",
            "S206SC0000",
            "S207SC0000",
            "S208SC0000",
            "S209SC0000",
            "S210SC0000",
            "S211SC0000",
            "S212SC0000",
            "S213SC0000",
            "S214SC0000",
            "S215SC0000",
            "S216SC0000",
            "S217SC0000",
            "S218SC0000",
            "S219SC0000",
            "S220SC0000",
            "S221SC0000",
            "S222SC0000",
            "S223SC0000",
            "S224SC0000",
            "S225SC0000",
            "S226SC0000",
            "S227SC0000",
            "S228SC0000",
            "S229SC0000",
            "S230SC0000",
            "S231SC0000",
            "S232SC0000",
            "S233SC0000",
            "S234SC0000",
            "S235SC0000",
            "S236SC0000",
            "S237SC0000",
            "S238SC0000",
            "S239SC0000",
            "S240SC0000",
            "S241SC0000",
            "S242SC0000",
            "S243SC0000",
            "S244SC0000",
            "S245SC0000",
            "S246SC0000",
            "S247SC0000",
            "S248SC0000",
            "S249SC0000",
            "S250SC0000",
            "S251SC0000",
            "S252SC0000",
            "S253SC0000",
            "S254SC0000",
            "S255SC0000",
            "S256SC0000",
            "S257SC0000",
            "S258SC0000",
            "S259SC0000"
        ]));
        Assert.That(viewModel.SelectedScenario, Is.EqualTo(viewModel.Scenarios[0]));
        Assert.That(viewModel.SelectedScenarioName, Is.EqualTo("5th Column"));
        Assert.That(
            viewModel.SelectedScenarioDescription,
            Is.EqualTo("The Fifth Column is the tower home of the inspirational leader who claims to hear the words of Gallean. The Elven faction devoted to Gallean do not get along with anyone including the Elven Alliance. This small realm is the GFC's home."));
        Assert.That(viewModel.SelectedScenarioObjective, Is.EqualTo("Elven Alliance is to capture both Rotturdamn and Gromdam.\nUndead Hordes are to capture both Silveria and Ellysium."));
        Assert.That(viewModel.SelectedScenarioLevel, Is.EqualTo("5"));
        Assert.That(viewModel.SelectedScenarioMapSize, Is.EqualTo("48 × 48"));
        Assert.That(viewModel.SelectedScenarioCityCount, Is.EqualTo("4"));
        Assert.That(viewModel.SelectedScenarioRaces, Is.EqualTo("Undead Hordes, Elven Alliance"));
        Assert.That(viewModel.SelectedScenarioCreator, Is.EqualTo("Emanuel Wall"));
    }

    [Test]
    public async Task StartGameTest()
    {
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();

        await gameViewModel.ShowScenarioSelectCommand.ExecuteAsync(null);

        ScenarioSelectScreenViewModel scenarioSelect = Assert.IsOfType<ScenarioSelectScreenViewModel>(gameViewModel.RootScreen);

        scenarioSelect.SelectedScenario = scenarioSelect.Scenarios[1];
        scenarioSelect.StartCommand.Execute(null);

        RaceSelectScreenViewModel raceSelect = Assert.IsOfType<RaceSelectScreenViewModel>(gameViewModel.TopScreen);

        Assert.That(FormatRaces(raceSelect.Races), Is.EqualTo([
            "Human Empire Human",
            "Dwarf Mountain Clans Computer",
            "Heretic Legions of the Damned Computer",
            "Elf Elven Alliance Computer"
        ]));

        raceSelect.Races[0].PlayerType = PlayerType.Computer;
        raceSelect.Races[2].PlayerType = PlayerType.Human;
        raceSelect.NextCommand.Execute(null);

        ScenarioConfigurationScreenViewModel scenarioConfiguration = Assert.IsOfType<ScenarioConfigurationScreenViewModel>(gameViewModel.TopScreen);

        scenarioConfiguration.SelectedLordCategory = LordCategory.Guildmaster;
        await gameViewModel.StartGameCommand.ExecuteAsync(scenarioConfiguration.GameConfiguration);

        BriefingScreenViewModel briefing = Assert.IsOfType<BriefingScreenViewModel>(gameViewModel.TopScreen);

        await briefing.ContinueCommand.ExecuteAsync(null);

        WorldScreenViewModel world = Assert.IsOfType<WorldScreenViewModel>(gameViewModel.RootScreen);
        GameSnapshot snapshot = world.GameModel.Snapshot;

        Assert.That(world.GameModel.ScenarioId, Is.EqualTo("S201SC0000"));
        Assert.That(FormatPlayers(snapshot.WorldState), Is.EqualTo([
            "1 Neutral G000LR0013 ai",
            "2 Human G000LR0001 ai",
            "3 Dwarf G000LR0004 ai",
            "4 Heretic g000LR0008 human",
            "5 Elf G000LR0016 ai"
        ]));
        Assert.That(snapshot.ViewPlayerId, Is.EqualTo(new WorldPlayerId(4)));
        Assert.That(scenarioSelect.IsOpen, Is.False);
        Assert.That(raceSelect.IsOpen, Is.False);
        Assert.That(scenarioConfiguration.IsOpen, Is.False);
    }
}
