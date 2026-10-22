using System.Collections.Immutable;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Gallean.Common;
using Gallean.Engine;

namespace Gallean.Client;

internal sealed partial class DiplomacyTabViewModel : ManagementTabViewModel
{
    private const int AmountStep = 50;
    private const int DefaultGift = 100;

    private DiplomacyOfferViewModel? selectedOffer;

    public DiplomacyTabViewModel(GameModel gameModel, GameViewModel gameViewModel)
        : base(gameModel, gameViewModel)
    {
        RefreshView();
    }

    public override string TabHeader => Tr("Diplomacy");

    [ObservableProperty]
    public partial IReadOnlyList<DiplomacyPlayerViewModel> Players { get; private set; } = [];

    [ObservableProperty]
    public partial DiplomacyPlayerViewModel? SelectedPlayer { get; private set; }

    [ObservableProperty]
    public partial IReadOnlyList<DiplomacyActionViewModel> Actions { get; private set; } = [];

    [ObservableProperty]
    public partial IReadOnlyList<DiplomacyOfferViewModel> Offers { get; private set; } = [];

    [ObservableProperty]
    public partial DiplomacyStep Step { get; private set; } = DiplomacyStep.Actions;

    [ObservableProperty]
    public partial string PromptText { get; private set; } = string.Empty;

    [ObservableProperty]
    public partial string AmountText { get; set; } = string.Empty;

    public void SelectPlayer(DiplomacyPlayerViewModel player)
    {
        Select(player);

        Step = DiplomacyStep.Actions;
    }

    public void SelectAction(DiplomacyActionViewModel action)
    {
        if (IsBusy || SelectedPlayer is null)
        {
            return;
        }

        DiplomacyPlayerViewModel player = SelectedPlayer;

        switch (action.Type)
        {
            case DiplomacyActionType.TradeItem:
                Offers = BuildItemOffers(player.Info);
                Step = DiplomacyStep.Offer;
                break;
            case DiplomacyActionType.TradeSpell:
                Offers = BuildSpellOffers(player.Info);
                Step = DiplomacyStep.Offer;
                break;
            case DiplomacyActionType.GiveGold:
                selectedOffer = null;
                PromptText = Tr("How much gold do you want to give the {0}?", player.RaceName);
                AmountText = DefaultGift.ToString();
                Step = DiplomacyStep.Amount;
                break;
            case DiplomacyActionType.ProposeAlliance:
                Game.OpenScreen(new ConfirmPromptViewModel(
                    Tr("Offer alliance"),
                    TrSpans("Do you want to ask the {0} for an alliance?", player.RaceName),
                    Tr("Offer"),
                    ConfirmPromptType.Primary,
                    () => ProposeAsync(player, new WorldDiplomacyProposeAllianceAction())));
                break;
            case DiplomacyActionType.BreakAlliance:
                Game.OpenScreen(new ConfirmPromptViewModel(
                    Tr("Break alliance"),
                    TrSpans("Breaking the alliance will greatly offend the {0}. Do you want to end the alliance and go to war?", player.RaceName),
                    Tr("Break"),
                    ConfirmPromptType.Primary,
                    () => BreakAllianceAsync(player)));
                break;
            case DiplomacyActionType.ExchangeMap:
                Game.OpenScreen(new ConfirmPromptViewModel(
                    Tr("Exchange map information"),
                    TrSpans("Do you want to ask the {0} to exchange maps?", player.RaceName),
                    Tr("Exchange"),
                    ConfirmPromptType.Primary,
                    () => ProposeAsync(player, new WorldDiplomacyExchangeMapAction())));
                break;
        }
    }

    public void SelectOffer(DiplomacyOfferViewModel offer)
    {
        selectedOffer = offer;
        PromptText = Tr("Name a price for {0}. It is worth {1} gold.", offer.Name, offer.Value);
        AmountText = offer.Value.ToString();
        Step = DiplomacyStep.Amount;
    }

    [RelayCommand]
    private void DecreaseAmount()
    {
        if (int.TryParse(AmountText, out int amount))
        {
            AmountText = Math.Max(amount - AmountStep, 0).ToString();
        }
        else
        {
            AmountText = "0";
        }
    }

    [RelayCommand]
    private void IncreaseAmount()
    {
        if (int.TryParse(AmountText, out int amount))
        {
            AmountText = (amount + AmountStep).ToString();
        }
        else
        {
            AmountText = AmountStep.ToString();
        }
    }

    [RelayCommand]
    private async Task ConfirmAmountAsync()
    {
        if (SelectedPlayer is null)
        {
            return;
        }

        if (!int.TryParse(AmountText, out int amount) || amount < 0)
        {
            Game.ShowError(Tr("Please enter a valid amount of gold"));

            return;
        }

        if (selectedOffer is not null)
        {
            await ProposeAsync(SelectedPlayer, selectedOffer.Action with { Price = amount });
        }
        else
        {
            await GiveGoldAsync(SelectedPlayer, amount);
        }
    }

    [RelayCommand]
    private void Back()
    {
        Step = DiplomacyStep.Actions;
    }

    protected override void OnSnapshotChanged(GameSnapshot current)
    {
        RefreshView();
    }

    private async Task GiveGoldAsync(DiplomacyPlayerViewModel player, int gold)
    {
        if (await TryDispatchWorldActionAsync(new WorldDiplomacyActionCommand(player.PlayerId, new WorldDiplomacyGiveGoldAction(gold))))
        {
            Game.ShowInfo(IconMoniker.Gold, Tr("Gold given"), Tr("{0} gold to the {1}", gold, player.RaceName));
            Step = DiplomacyStep.Actions;
        }
    }

    private async Task BreakAllianceAsync(DiplomacyPlayerViewModel player)
    {
        if (await TryDispatchWorldActionAsync(new WorldDiplomacyActionCommand(player.PlayerId, new WorldDiplomacyBreakAllianceAction())))
        {
            Game.ShowInfo(IconMoniker.Banner, Tr("Alliance broken"), Tr("At war with the {0}", player.RaceName));
            Step = DiplomacyStep.Actions;
        }
    }

    private async Task ProposeAsync(DiplomacyPlayerViewModel player, WorldDiplomacyProposalAction action)
    {
        ICommandResult<WorldCommandError, WorldEvent, Empty>? result =
            await DispatchWorldActionAsync(new WorldDiplomacyActionCommand(player.PlayerId, action));

        if (result is null)
        {
            return;
        }

        WorldDiplomacyProposalAnsweredEvent? answered = result.Events.OfType<WorldDiplomacyProposalAnsweredEvent>().FirstOrDefault();

        if (answered is not null)
        {
            string answer = DiplomacyProposalFormatter.FormatAnswer(GameModel.Definitions, GameModel.Texts, WorldState, answered.Proposal, answered.Accepted);

            if (answered.Accepted)
            {
                Game.ShowInfo(IconMoniker.Exchange, Tr("Offer accepted"), answer);
            }
            else
            {
                Game.ShowInfo(IconMoniker.Exchange, Tr("Offer declined"), answer);
            }
        }
        else
        {
            Game.ShowInfo(IconMoniker.Exchange, Tr("Offer sent"), Tr("Awaiting the {0}'s answer", player.RaceName));
        }

        Step = DiplomacyStep.Actions;
    }

    private void RefreshView()
    {
        WorldPlayerId? selectedPlayerId = SelectedPlayer?.PlayerId;

        Players = BuildPlayers();

        Select(Players.FirstOrDefault(player => player.PlayerId == selectedPlayerId) ?? Players.FirstOrDefault());
    }

    private void Select(DiplomacyPlayerViewModel? player)
    {
        if (SelectedPlayer is not null)
        {
            SelectedPlayer.IsSelected = false;
        }

        if (player is not null)
        {
            player.IsSelected = true;
            Actions = BuildActions(player.Info);
        }
        else
        {
            Actions = [];
        }

        SelectedPlayer = player;
    }

    private ImmutableArray<WorldGetDiplomacyInfoCommand.Player> ResolvePlayers()
    {
        if (!Snapshot.IsViewPlayerTurn)
        {
            return [];
        }

        ICommandResult<WorldCommandError, WorldEvent, WorldGetDiplomacyInfoCommand.Result> result =
            GameModel.World.DryRunCommand(new WorldGetDiplomacyInfoCommand());

        if (result.Failed)
        {
            return [];
        }
        else
        {
            return result.Data.Players;
        }
    }

    private IReadOnlyList<DiplomacyPlayerViewModel> BuildPlayers()
    {
        List<DiplomacyPlayerViewModel> players = [];

        foreach (WorldGetDiplomacyInfoCommand.Player info in ResolvePlayers())
        {
            WorldPlayer player = WorldState.Players[info.PlayerId];

            players.Add(new DiplomacyPlayerViewModel(
                this,
                info,
                WorldPlayerFormatter.FormatLordName(GameModel.Texts, player),
                WorldPlayerFormatter.FormatRaceName(GameModel.Definitions, GameModel.Texts, player),
                new AssetSpriteKey.LordFace(player.Race, player.FaceVariant.Value, IsLarge: false),
                new AssetSpriteKey.LordFace(player.Race, player.FaceVariant.Value, IsLarge: true)));
        }

        return players;
    }

    private IReadOnlyList<DiplomacyActionViewModel> BuildActions(WorldGetDiplomacyInfoCommand.Player info)
    {
        string? tradeItemReason = null;
        string? tradeSpellReason = null;

        if (info.Items.IsEmpty)
        {
            tradeItemReason = Tr("You have no items to trade");
        }

        if (!info.Spells.Any(spell => spell.Error is null))
        {
            tradeSpellReason = Tr("You have no spells available to sell.");
        }

        DiplomacyActionViewModel[] actions =
        [
            new(
                this,
                DiplomacyActionType.TradeItem,
                Tr("Sell item"),
                Tr("Sell an item from one of your cities for a price you name."),
                tradeItemReason),
            new(
                this,
                DiplomacyActionType.TradeSpell,
                Tr("Sell spell"),
                Tr("Sell a spell you know for a price you name."),
                tradeSpellReason),
            new(
                this,
                DiplomacyActionType.GiveGold,
                Tr("Give gold"),
                Tr("A gift of gold improves relations."),
                FormatUnavailableReason(info.GiveGoldError)),
            new(
                this,
                DiplomacyActionType.ProposeAlliance,
                Tr("Offer alliance"),
                Tr("Propose an alliance. Allies share knowledge of each other's armies and cannot fight one another."),
                FormatUnavailableReason(info.ProposeAllianceError)),
            new(
                this,
                DiplomacyActionType.BreakAlliance,
                Tr("Break alliance"),
                Tr("End the treaty and go to war."),
                FormatUnavailableReason(info.BreakAllianceError)),
            new(
                this,
                DiplomacyActionType.ExchangeMap,
                Tr("Exchange map information"),
                Tr("Exchange maps once per turn, revealing territory explored by either side."),
                FormatUnavailableReason(info.ExchangeMapError))
        ];

        return [.. actions.OrderBy(action => !action.Status.IsAvailable)];
    }

    private IReadOnlyList<DiplomacyOfferViewModel> BuildItemOffers(WorldGetDiplomacyInfoCommand.Player info)
    {
        List<DiplomacyOfferViewModel> offers = [];

        foreach (WorldGetDiplomacyInfoCommand.TradeItem item in info.Items)
        {
            ItemDefinition definition = GameModel.Definitions.GetItem(item.ItemId);

            offers.Add(new DiplomacyOfferViewModel(
                this,
                new WorldDiplomacyTradeItemAction(item.SourceFortificationId, item.ItemId, item.Value),
                new IconViewModel.Item(ItemIconResolver.Resolve(GameModel.Definitions, item.ItemId)),
                GameModel.Texts.GetGlobalText(definition.NameTextId).Text,
                GameModel.Texts.GetGlobalText(definition.DescriptionTextId).Text,
                item.Value,
                FormatUnavailableReason(item.Error)));
        }

        return offers;
    }

    private IReadOnlyList<DiplomacyOfferViewModel> BuildSpellOffers(WorldGetDiplomacyInfoCommand.Player info)
    {
        List<DiplomacyOfferViewModel> offers = [];

        foreach (WorldGetDiplomacyInfoCommand.TradeSpell spell in info.Spells)
        {
            WorldSpellDefinition definition = GameModel.Definitions.GetSpell(spell.SpellId);

            offers.Add(new DiplomacyOfferViewModel(
                this,
                new WorldDiplomacyTradeSpellAction(spell.SpellId, spell.Value),
                SpellIconResolver.Resolve(GameModel.Definitions, spell.SpellId),
                GameModel.Texts.GetGlobalText(definition.NameTextId).Text,
                GameModel.Texts.GetGlobalText(definition.DescriptionTextId).Text,
                spell.Value,
                FormatUnavailableReason(spell.Error)));
        }

        return offers;
    }

    private static string? FormatUnavailableReason(WorldCommandError? error)
    {
        if (error is not null)
        {
            return WorldCommandErrorFormatter.Format(error.Value);
        }
        else
        {
            return null;
        }
    }
}
