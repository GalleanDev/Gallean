using System.ComponentModel;
using Gallean.Common;

namespace Gallean.Client;

internal sealed partial class WorldScreenViewModel
{
    private sealed class EventActionPlayer(WorldScreenViewModel parent) :
        IWorldEventActionVisitor<Empty>,
        IWorldEventScreenVisitor<AbstractScreenViewModel>
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

        Empty IWorldEventActionVisitor<Empty>.Visit(WorldEventAction.StackExhaust action)
        {
            parent.OnActionStackExhaust(action);

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
            AbstractScreenViewModel screen = action.Accept(this);

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

        AbstractScreenViewModel IWorldEventScreenVisitor<AbstractScreenViewModel>.Visit(WorldEventAction.Loading action)
        {
            return new LoadingScreenViewModel(Tr("Loading world…"), TimeSpan.Zero, TimeSpan.Zero);
        }

        AbstractScreenViewModel IWorldEventScreenVisitor<AbstractScreenViewModel>.Visit(WorldEventAction.HotseatTurn action)
        {
            return new HotseatTurnScreenViewModel(action.LordName, action.RaceName, action.PortraitKey);
        }

        AbstractScreenViewModel IWorldEventScreenVisitor<AbstractScreenViewModel>.Visit(WorldEventAction.ScenarioPopup action)
        {
            return new ScenarioEventPopupViewModel(action.Messages);
        }

        AbstractScreenViewModel IWorldEventScreenVisitor<AbstractScreenViewModel>.Visit(WorldEventAction.RuinLoot action)
        {
            return LootScreenViewModel.CreateForRuin(parent.GameModel, action.Resources, action.Item);
        }

        AbstractScreenViewModel IWorldEventScreenVisitor<AbstractScreenViewModel>.Visit(WorldEventAction.ItemsReceive action)
        {
            return LootScreenViewModel.CreateForItems(parent.GameModel, action.Items);
        }

        AbstractScreenViewModel IWorldEventScreenVisitor<AbstractScreenViewModel>.Visit(WorldEventAction.SpellsReceive action)
        {
            return LootScreenViewModel.CreateForSpells(parent.GameModel, action.Spells);
        }

        AbstractScreenViewModel IWorldEventScreenVisitor<AbstractScreenViewModel>.Visit(WorldEventAction.LeaderUpgrade action)
        {
            return new LeaderUpgradeViewModel(parent.GameModel, parent.Game, action.StackId);
        }

        AbstractScreenViewModel IWorldEventScreenVisitor<AbstractScreenViewModel>.Visit(WorldEventAction.DiplomacyProposal action)
        {
            return new DiplomacyProposalViewModel(parent.GameModel, parent.Game, action.Proposal);
        }

        AbstractScreenViewModel IWorldEventScreenVisitor<AbstractScreenViewModel>.Visit(WorldEventAction.ThiefAction action)
        {
            return new ThiefActionScreenViewModel(parent.GameModel, parent.Game, action.StackId, action.TargetPosition);
        }

        AbstractScreenViewModel IWorldEventScreenVisitor<AbstractScreenViewModel>.Visit(WorldEventAction.ThiefActionResult action)
        {
            return new ThiefActionResultScreenViewModel(action.Type, action.IsSuccess);
        }

        AbstractScreenViewModel IWorldEventScreenVisitor<AbstractScreenViewModel>.Visit(WorldEventAction.SiteEnter action)
        {
            return ManagementScreenViewModel.CreateForSite(parent.GameModel, parent.Game, action.StackId);
        }
    }
}
