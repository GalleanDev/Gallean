using System.ComponentModel;
using Gallean.Common;

namespace Gallean.Client;

internal sealed partial class WorldScreenViewModel
{
    private sealed class EventActionPlayer(WorldScreenViewModel parent) : IWorldEventActionVisitor<Empty>
    {
        public WorldAnimationState? ActiveAnimation { get; private set; }
        public AbstractScreenViewModel? ActiveScreen { get; private set; }

        public void Stop()
        {
            ActiveAnimation = null;
            ActiveScreen = null;
        }

        Empty IWorldEventActionVisitor<Empty>.Visit(WorldEventAction.OneShot action)
        {
            return Empty.Value;
        }

        Empty IWorldEventActionVisitor<Empty>.Visit(WorldEventAction.AutoSave action)
        {
            parent.OnActionAutoSave(action);

            return Empty.Value;
        }

        Empty IWorldEventActionVisitor<Empty>.Visit(WorldEventAction.TurnStart action)
        {
            parent.OnActionTurnStart(action);

            return Empty.Value;
        }

        Empty IWorldEventActionVisitor<Empty>.Visit(WorldEventAction.ObjectiveChange action)
        {
            parent.OnActionObjectiveChange(action);

            return Empty.Value;
        }

        Empty IWorldEventActionVisitor<Empty>.Visit(WorldEventAction.FortificationExit action)
        {
            parent.OnActionFortificationExit(action);

            return Empty.Value;
        }

        Empty IWorldEventActionVisitor<Empty>.Visit(WorldEventAction.Animation action)
        {
            ActiveAnimation = action.State;

            return Empty.Value;
        }

        Empty IWorldEventActionVisitor<Empty>.Visit(WorldEventAction.Battle action)
        {
            ActiveAnimation = new WorldAnimationState.StackBattle(action.Position);
            PlayBattleAsync(action.Position).FileAndForget();

            return Empty.Value;
        }

        Empty IWorldEventActionVisitor<Empty>.Visit(WorldEventAction.Screen action)
        {
            AbstractScreenViewModel screen = CreateScreen(action);

            ActiveScreen = screen;
            parent.Game.OpenScreen(screen);

            return Empty.Value;
        }

        private async Task PlayBattleAsync(WorldPosition position)
        {
            BattleScreenViewModel? screen = await parent.Game.ShowBattleAsync(parent);

            if (screen is not null)
            {
                ActiveAnimation = new WorldAnimationState.BattleStart(position);
                parent.RefreshRenderRequest();

                await WaitTransitionOutAsync();

                ActiveAnimation = new WorldAnimationState.BattleEnd(position);
                parent.RefreshRenderRequest();
            }

            async Task WaitTransitionOutAsync()
            {
                TaskCompletionSource completionSource = new();

                screen.PropertyChanged += OnTransitionChanged;
                try
                {
                    await completionSource.Task;
                }
                finally
                {
                    screen.PropertyChanged -= OnTransitionChanged;
                }

                return;

                void OnTransitionChanged(object? sender, PropertyChangedEventArgs e)
                {
                    if (screen.TransitionState is { Type: BattleTransitionType.Out })
                    {
                        completionSource.TrySetResult();
                    }
                }
            }
        }

        private AbstractScreenViewModel CreateScreen(WorldEventAction.Screen action)
        {
            return action switch
            {
                WorldEventAction.Loading => new LoadingScreenViewModel(Tr("Loading world…"), TimeSpan.Zero, TimeSpan.Zero),
                WorldEventAction.HotseatTurn hotseatTurn => new HotseatTurnScreenViewModel(hotseatTurn.LordName, hotseatTurn.RaceName, hotseatTurn.PortraitKey),
                WorldEventAction.ScenarioPopup popup => new ScenarioEventPopupViewModel(popup.Messages),
                WorldEventAction.RuinLoot loot => LootScreenViewModel.CreateForRuin(parent.GameModel, loot.Resources, loot.Item),
                WorldEventAction.ItemsReceive items => LootScreenViewModel.CreateForItems(parent.GameModel, items.Items),
                WorldEventAction.SpellsReceive spells => LootScreenViewModel.CreateForSpells(parent.GameModel, spells.Spells),
                WorldEventAction.ResourcesCapture capture => LootScreenViewModel.CreateForCapturedResources(capture.Resources),
                WorldEventAction.LeaderUpgrade upgrade => new LeaderUpgradeViewModel(parent.GameModel, parent.Game, upgrade.StackId),
                WorldEventAction.DiplomacyProposal diplomacy => new DiplomacyProposalViewModel(parent.GameModel, parent.Game, diplomacy.Proposal),
                WorldEventAction.ThiefAction thief => new ThiefActionScreenViewModel(parent.GameModel, parent.Game, thief.StackId, thief.TargetPosition),
                WorldEventAction.ThiefActionMineResult mine => ThiefActionResultScreenViewModel.CreateMineResult(mine.Type, mine.ActionName, mine.IsSuccess),
                WorldEventAction.ThiefActionTheirResult their => ThiefActionResultScreenViewModel.CreateTheirResult(their.Type, their.ActionName, their.Target, their.IsSuccess, their.TheirLordName),
                WorldEventAction.SiteEnter site => ManagementScreenViewModel.CreateForSite(parent.GameModel, parent.Game, site.StackId)
            };
        }
    }
}
