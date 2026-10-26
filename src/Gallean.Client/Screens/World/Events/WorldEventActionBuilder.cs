using System.Collections.Immutable;
using Gallean.Common;
using Gallean.Configuration;
using Gallean.Engine;
using static Gallean.Client.WorldEventQueue;

namespace Gallean.Client;

internal sealed class WorldEventActionBuilder(
    IGameDefinitionProvider definitions,
    IGameTextProvider texts,
    IGameScenarioProvider scenario,
    IRandomProvider random,
    GameSnapshot snapshot,
    BattleModel? activeBattle,
    GameSettings settings,
    bool fogEnabled)
{
    private WorldPlayerId ViewPlayerId => snapshot.ViewPlayerId;

    public WorldEventAction Build(Entry entry)
    {
        return entry switch
        {
            Entry.StackMove data => BuildStackMove(data),
            Entry.StackDestroy data => BuildStackDestroy(data),
            Entry.StackRemove data => BuildStackRemove(data),
            Entry.StackCreate data => BuildStackCreate(data),
            Entry.StackOwnerChange data => BuildStackOwnerChange(data),
            Entry.MountainRemove data => BuildMountainRemove(data),
            Entry.Unfog data => BuildUnfog(data),
            Entry.SpellCast data => BuildSpellCast(data),
            Entry.GlobalSpellCast data => BuildGlobalSpellCast(data),
            Entry.ScenarioPopup data => BuildScenarioPopup(data),
            Entry.ObjectiveChange data => BuildObjectiveChange(data),
            Entry.StackIdle data => BuildStackIdle(data),
            Entry.StackExhaust data => BuildStackExhaust(data),
            Entry.TurnStart data => BuildTurnStart(data),
            Entry.HotseatTurn data => BuildHotseatTurn(data),
            Entry.ScenarioEnd data => BuildScenarioEnd(data),
            Entry.CityRiotEnd data => BuildCityRiotEnd(data),
            Entry.FortificationEnter data => BuildFortificationEnter(data),
            Entry.FortificationExit data => BuildFortificationExit(data),
            Entry.RuinLoot data => BuildRuinLoot(data),
            Entry.BagPickUp data => BuildBagPickUp(data),
            Entry.ItemsReceive data => BuildItemsReceive(data),
            Entry.SpellsReceive data => BuildSpellsReceive(data),
            Entry.LeaderUpgrade data => BuildLeaderUpgrade(data),
            Entry.DiplomacyProposal data => BuildDiplomacyProposal(data),
            Entry.ThiefAction data => BuildThiefAction(data),
            Entry.ThiefActionResult data => BuildThiefActionResult(data),
            Entry.SiteEnter data => BuildSiteEnter(data),
            Entry.Pause data => BuildPause(data),
            Entry.Battle data => BuildBattle(data),
            Entry.GameStart data => BuildGameStart(data),
            _ => throw new ArgumentOutOfRangeException(nameof(entry), entry, null)
        };
    }

    private WorldEventAction BuildStackMove(Entry.StackMove stackMove)
    {
        WorldVisibility visibility = WorldVisibility.Create(stackMove.State, ViewPlayerId, fogEnabled);

        if (visibility.IsAnyRevealed(stackMove.Steps.Select(step => step.Position)))
        {
            return Animate(stackMove, visibility);
        }
        else
        {
            return WorldEventAction.None;
        }
    }

    private WorldEventAction BuildStackDestroy(Entry.StackDestroy stackDestroy)
    {
        WorldVisibility visibility = WorldVisibility.Create(stackDestroy.State, ViewPlayerId, fogEnabled);

        if (visibility.IsRevealed(stackDestroy.Stack.Position))
        {
            (WorldUnit leader, GridPosition _, UnitDefinition _) = stackDestroy.Stack.GetLeader(definitions);
            WorldEventAction.Animation animation = Animate(stackDestroy, visibility);

            return animation with { Sounds = [new AssetSoundKey.StackUnitDeath(leader.DefinitionId)] };
        }
        else
        {
            return WorldEventAction.None;
        }
    }

    private WorldEventAction BuildStackRemove(Entry.StackRemove stackRemove)
    {
        WorldVisibility visibility = WorldVisibility.Create(stackRemove.State, ViewPlayerId, fogEnabled);

        if (visibility.IsRevealed(stackRemove.Stack.Position))
        {
            return Animate(stackRemove, visibility);
        }
        else
        {
            return WorldEventAction.None;
        }
    }

    private WorldEventAction BuildStackCreate(Entry.StackCreate stackCreate)
    {
        WorldVisibility visibility = WorldVisibility.Create(stackCreate.State, ViewPlayerId, fogEnabled);

        if (visibility.IsRevealed(stackCreate.Stack.Position))
        {
            return Animate(stackCreate, visibility);
        }
        else
        {
            return WorldEventAction.None;
        }
    }

    private WorldEventAction BuildStackOwnerChange(Entry.StackOwnerChange stackOwnerChange)
    {
        WorldVisibility visibility = WorldVisibility.Create(stackOwnerChange.State, ViewPlayerId, fogEnabled);

        if (visibility.IsRevealed(stackOwnerChange.Position))
        {
            return Animate(stackOwnerChange, visibility);
        }
        else
        {
            return WorldEventAction.None;
        }
    }

    private WorldEventAction BuildMountainRemove(Entry.MountainRemove mountainRemove)
    {
        WorldVisibility visibility = WorldVisibility.Create(mountainRemove.State, ViewPlayerId, fogEnabled);

        if (visibility.IsRevealed(mountainRemove.Center))
        {
            return Animate(mountainRemove, visibility);
        }
        else
        {
            return WorldEventAction.None;
        }
    }

    private WorldEventAction BuildUnfog(Entry.Unfog unfog)
    {
        if (unfog.PlayerId == ViewPlayerId)
        {
            WorldVisibility visibility = WorldVisibility.Create(unfog.State, ViewPlayerId, fogEnabled);

            return Animate(unfog, visibility);
        }
        else
        {
            return WorldEventAction.None;
        }
    }

    private WorldEventAction BuildSpellCast(Entry.SpellCast spellCast)
    {
        WorldVisibility visibility = WorldVisibility.Create(spellCast.State, ViewPlayerId, fogEnabled);

        if (visibility.IsRevealed(spellCast.Position))
        {
            return Animate(spellCast, visibility);
        }
        else
        {
            return WorldEventAction.None;
        }
    }

    private WorldEventAction BuildGlobalSpellCast(Entry.GlobalSpellCast spellCast)
    {
        WorldVisibility visibility = WorldVisibility.Create(spellCast.State, ViewPlayerId, fogEnabled);

        if (visibility.IsRevealed(spellCast.Position))
        {
            return Animate(spellCast, visibility);
        }
        else
        {
            WorldPlan plan = WorldPlan.For(spellCast.State);
            WorldCapital? capital = plan.GetCapital(ViewPlayerId);

            if (capital is not null)
            {
                return Animate(spellCast with { Position = capital.Entrance }, visibility);
            }
            else
            {
                return WorldEventAction.None;
            }
        }
    }

    private WorldEventAction BuildPause(Entry.Pause pause)
    {
        WorldPosition? focusPosition = pause.GetFocusPosition(ViewPlayerId);

        if (focusPosition is null)
        {
            return WorldEventAction.None;
        }

        WorldVisibility visibility = WorldVisibility.Create(pause.State, ViewPlayerId, fogEnabled);

        if (visibility.IsRevealed(focusPosition.Value))
        {
            return Animate(pause, visibility);
        }
        else
        {
            return WorldEventAction.None;
        }
    }

    private WorldEventAction BuildBattle(Entry.Battle battle)
    {
        if (activeBattle is not null && activeBattle.Position == battle.Position)
        {
            WorldStack attacker = battle.State.Stacks[battle.AttackerStackId];
            (WorldUnit leader, GridPosition _, UnitDefinition _) = attacker.GetLeader(definitions);

            return new WorldEventAction.Battle(
                [new AssetSoundKey.StackUnitAttack(leader.DefinitionId), new AssetSoundKey.WorldBattleStart()],
                battle.GetFocusPosition(ViewPlayerId),
                battle.Position);
        }
        else
        {
            return WorldEventAction.None;
        }
    }

    private WorldEventAction BuildGameStart(Entry.GameStart gameStart)
    {
        return new WorldEventAction.Loading([], gameStart.GetFocusPosition(ViewPlayerId));
    }

    private WorldEventAction BuildScenarioPopup(Entry.ScenarioPopup scenarioPopup)
    {
        if (scenarioPopup.Messages[0].Audience.Contains(ViewPlayerId))
        {
            ScenarioTextFormatter formatter = new(scenario.Names);
            ImmutableArray<ScenarioEventPopupMessage> messages =
            [
                .. scenarioPopup.Messages.Select(message => ScenarioEventPopupMessage.Create(texts, definitions, formatter, message))
            ];

            return new WorldEventAction.ScenarioPopup([], FocusPosition: null, messages);
        }
        else
        {
            return WorldEventAction.None;
        }
    }

    private WorldEventAction BuildObjectiveChange(Entry.ObjectiveChange objectiveChange)
    {
        string text = texts.GetScenarioText(objectiveChange.TextId);
        string objective = ScenarioFormatter.FormatObjective(text);

        return new WorldEventAction.ObjectiveChange([], objective);
    }

    private WorldEventAction BuildStackIdle(Entry.StackIdle stackIdle)
    {
        if (snapshot.WorldState.Stacks.TryGetValue(stackIdle.StackId, out WorldStack? stack))
        {
            (WorldUnit leader, GridPosition _, UnitDefinition _) = stack.GetLeader(definitions);
            int variant = random.Next(AssetSoundKey.StackUnitIdle.VariantCount);

            return Sound(new AssetSoundKey.StackUnitIdle(leader.DefinitionId, variant));
        }
        else
        {
            return WorldEventAction.None;
        }
    }

    private WorldEventAction BuildStackExhaust(Entry.StackExhaust exhaust)
    {
        if (exhaust.State.Stacks.TryGetValue(exhaust.StackId, out WorldStack? stack) && stack.OwnerId == ViewPlayerId)
        {
            return new WorldEventAction.StackExhaust([], stack.Id);
        }
        else
        {
            return WorldEventAction.None;
        }
    }

    private WorldEventAction BuildTurnStart(Entry.TurnStart turnStart)
    {
        if (turnStart.PlayerId == ViewPlayerId)
        {
            AssetSoundKey sound = new AssetSoundKey.TurnStart();

            WorldEventAction.AutoSave? autoSave = null;

            if (snapshot.IsSaveAllowed && IsAutoSaveTurn(turnStart.State.TurnInfo.TurnNumber))
            {
                autoSave = new WorldEventAction.AutoSave([], GameSaveType.Turn, Tr("Autosave"));
            }

            return new WorldEventAction.TurnStart([sound], autoSave);
        }
        else
        {
            return WorldEventAction.None;
        }

        bool IsAutoSaveTurn(int turnNumber)
        {
            if (settings.AutoSaveInterval > 0)
            {
                return turnNumber % settings.AutoSaveInterval == 0;
            }
            else
            {
                return false;
            }
        }
    }

    private WorldEventAction BuildHotseatTurn(Entry.HotseatTurn hotseatTurn)
    {
        WorldPlayer player = hotseatTurn.State.Players[hotseatTurn.PlayerId];

        return new WorldEventAction.HotseatTurn(
            [],
            FocusPosition: null,
            WorldPlayerFormatter.FormatLordName(texts, player),
            WorldPlayerFormatter.FormatRaceName(definitions, texts, player),
            new AssetSpriteKey.LordFace(player.Race, player.FaceVariant.Value, IsLarge: true));
    }

    private WorldEventAction BuildScenarioEnd(Entry.ScenarioEnd scenarioEnd)
    {
        if (scenarioEnd.WinnerId is not null && scenarioEnd.WinnerId.Value == ViewPlayerId)
        {
            return new WorldEventAction.AutoSave([], GameSaveType.Victory, Tr("Victory"));
        }
        else
        {
            return WorldEventAction.None;
        }
    }

    private WorldEventAction BuildCityRiotEnd(Entry.CityRiotEnd riotEnd)
    {
        WorldPlan plan = WorldPlan.For(snapshot.WorldState);
        WorldVisibility visibility = WorldVisibility.Create(snapshot.WorldState, ViewPlayerId, fogEnabled);
        WorldFortification? fortification = visibility.GetFortificationAt(plan, riotEnd.Position);

        if (fortification is not null)
        {
            return Sound(new AssetSoundKey.CityRiotEnd());
        }
        else
        {
            return WorldEventAction.None;
        }
    }

    private WorldEventAction BuildFortificationEnter(Entry.FortificationEnter enter)
    {
        WorldPlan plan = WorldPlan.For(snapshot.WorldState);
        WorldVisibility visibility = WorldVisibility.Create(snapshot.WorldState, ViewPlayerId, fogEnabled);
        WorldFortification? fortification = visibility.GetFortificationAt(plan, enter.Position);

        return fortification switch
        {
            WorldCapital => Sound(new AssetSoundKey.CityEnter()),
            WorldCity => Sound(new AssetSoundKey.CityEnter()),
            _ => WorldEventAction.None
        };
    }

    private WorldEventAction BuildFortificationExit(Entry.FortificationExit exit)
    {
        WorldPlan plan = WorldPlan.For(snapshot.WorldState);
        WorldVisibility visibility = WorldVisibility.Create(snapshot.WorldState, ViewPlayerId, fogEnabled);
        WorldFortification? fortification = visibility.GetFortificationAt(plan, exit.Position);

        if (fortification is not (WorldCity or WorldCapital))
        {
            return WorldEventAction.None;
        }

        if (exit.State.Stacks.TryGetValue(exit.StackId, out WorldStack? stack) && stack.OwnerId == ViewPlayerId)
        {
            return new WorldEventAction.FortificationExit([new AssetSoundKey.CityExit()], stack.Id);
        }
        else
        {
            return Sound(new AssetSoundKey.CityExit());
        }
    }

    private WorldEventAction BuildRuinLoot(Entry.RuinLoot ruinLoot)
    {
        WorldPlan plan = WorldPlan.For(snapshot.WorldState);
        WorldVisibility visibility = WorldVisibility.Create(snapshot.WorldState, ViewPlayerId, fogEnabled);
        WorldFortification? fortification = visibility.GetFortificationAt(plan, ruinLoot.Position);

        if (fortification is null)
        {
            return WorldEventAction.None;
        }

        WorldStack looter = ruinLoot.State.Stacks[ruinLoot.StackId];

        if (looter.OwnerId == ViewPlayerId)
        {
            return new WorldEventAction.RuinLoot(
                [new AssetSoundKey.RuinEnter(), new AssetSoundKey.RuinLoot()],
                ruinLoot.GetFocusPosition(ViewPlayerId),
                ruinLoot.Resources,
                ruinLoot.Item);
        }
        else
        {
            return Sound(new AssetSoundKey.RuinLoot());
        }
    }

    private WorldEventAction BuildBagPickUp(Entry.BagPickUp bagPickUp)
    {
        WorldStack stack = bagPickUp.State.Stacks[bagPickUp.StackId];

        if (stack.OwnerId == ViewPlayerId && bagPickUp.Items.Items.Length > 0)
        {
            return new WorldEventAction.ItemsReceive(
                [new AssetSoundKey.BagPickUp()],
                bagPickUp.GetFocusPosition(ViewPlayerId),
                bagPickUp.Items);
        }
        else
        {
            return WorldEventAction.None;
        }
    }

    private WorldEventAction BuildItemsReceive(Entry.ItemsReceive itemsReceive)
    {
        if (itemsReceive.PlayerId == ViewPlayerId)
        {
            return new WorldEventAction.ItemsReceive(
                [new AssetSoundKey.BagPickUp()],
                itemsReceive.GetFocusPosition(ViewPlayerId),
                new WorldInventory(itemsReceive.Items));
        }
        else
        {
            return WorldEventAction.None;
        }
    }

    private WorldEventAction BuildSpellsReceive(Entry.SpellsReceive spellsReceive)
    {
        if (spellsReceive.PlayerId == ViewPlayerId)
        {
            return new WorldEventAction.SpellsReceive(
                [new AssetSoundKey.BagPickUp()],
                spellsReceive.GetFocusPosition(ViewPlayerId),
                spellsReceive.Spells);
        }
        else
        {
            return WorldEventAction.None;
        }
    }

    private WorldEventAction BuildLeaderUpgrade(Entry.LeaderUpgrade upgrade)
    {
        WorldState state = snapshot.WorldState;

        if (!state.Stacks.TryGetValue(upgrade.StackId, out WorldStack? stack))
        {
            return WorldEventAction.None;
        }

        if (stack.OwnerId != ViewPlayerId || stack.PendingLeaderUpgrades <= 0 || stack.LeaderUpgradePostponed)
        {
            return WorldEventAction.None;
        }

        (WorldUnit _, GridPosition _, UnitDefinition leaderDefinition) = stack.GetLeader(definitions);

        if (leaderDefinition.Race == state.Players[stack.OwnerId].Race &&
            leaderDefinition.LeaderCategory != UnitLeaderCategory.Thief)
        {
            return new WorldEventAction.LeaderUpgrade([], upgrade.GetFocusPosition(ViewPlayerId), stack.Id);
        }
        else
        {
            return WorldEventAction.None;
        }
    }

    private WorldEventAction BuildThiefAction(Entry.ThiefAction entry)
    {
        if (!snapshot.WorldState.Stacks.TryGetValue(entry.StackId, out WorldStack? stack))
        {
            return WorldEventAction.None;
        }

        if (stack.OwnerId == ViewPlayerId && snapshot.IsViewPlayerTurn && stack.Position.IsAdjacentTo(entry.TargetPosition))
        {
            return new WorldEventAction.ThiefAction([], entry.GetFocusPosition(ViewPlayerId), stack.Id, entry.TargetPosition);
        }
        else
        {
            return WorldEventAction.None;
        }
    }

    private WorldEventAction BuildThiefActionResult(Entry.ThiefActionResult result)
    {
        if (result.PlayerId == ViewPlayerId)
        {
            return new WorldEventAction.ThiefActionResult(
                [new AssetSoundKey.ThiefAction()],
                result.GetFocusPosition(ViewPlayerId),
                result.Type,
                result.IsSuccess);
        }
        else
        {
            WorldVisibility visibility = WorldVisibility.Create(result.State, ViewPlayerId, fogEnabled);

            if (visibility.IsRevealed(result.Position))
            {
                return Animate(result, visibility);
            }
            else
            {
                return WorldEventAction.None;
            }
        }
    }

    private WorldEventAction BuildSiteEnter(Entry.SiteEnter entry)
    {
        if (!snapshot.WorldState.Stacks.TryGetValue(entry.StackId, out WorldStack? stack))
        {
            return WorldEventAction.None;
        }

        WorldSite site = snapshot.WorldState.Sites[entry.SiteId];

        if (stack.OwnerId == ViewPlayerId && snapshot.IsViewPlayerTurn && stack.Position == site.Entrance)
        {
            return new WorldEventAction.SiteEnter(
                [new AssetSoundKey.SiteEnter()],
                entry.GetFocusPosition(ViewPlayerId),
                stack.Id,
                site.Id);
        }
        else
        {
            return WorldEventAction.None;
        }
    }

    private WorldEventAction BuildDiplomacyProposal(Entry.DiplomacyProposal entry)
    {
        WorldDiplomacyProposal proposal = entry.Proposal;

        if (proposal.TargetPlayerId != ViewPlayerId)
        {
            return WorldEventAction.None;
        }

        WorldPlayer viewPlayer = snapshot.WorldState.Players[ViewPlayerId];

        if (viewPlayer.IncomingDiplomacyProposals.Contains(proposal))
        {
            return new WorldEventAction.DiplomacyProposal([], entry.GetFocusPosition(ViewPlayerId), proposal);
        }
        else
        {
            return WorldEventAction.None;
        }
    }

    private WorldEventAction.Animation Animate(Entry.Animated entry, WorldVisibility visibility)
    {
        return new WorldEventAction.Animation(
            [],
            entry.GetFocusPosition(ViewPlayerId),
            entry.CreateAnimation(definitions, visibility, settings));
    }

    private static WorldEventAction Sound(AssetSoundKey sound)
    {
        return new WorldEventAction.OneShot([sound]);
    }
}
