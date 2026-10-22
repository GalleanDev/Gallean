using Gallean.Tests.Common;

namespace Gallean.Engine.Tests;

public sealed class BattleSessionTests
{
    [Test]
    public void AiControlledSideTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 1);

        WorldPlayer human = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer ai = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map) with { IsAi = true };

        UnitDefinition attackerDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("attacker", maxHitpoints: 10, initiative: 50, damage: 5),
            movementPoints: 6, leadership: 6);
        UnitDefinition defenderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 10, initiative: 40, damage: 5),
            movementPoints: 6, leadership: 6);

        WorldStack attackerStack = TestHelpers.SimpleStack(
            new WorldStackId("attacker"),
            human.Id,
            new WorldPosition(0, 0),
            new UnitGrid<WorldUnit>.Builder().Place(TestHelpers.SimpleUnit(new WorldUnitId("attacker"), attackerDefinition), GridPosition.FrontBottom, false).Build(),
            movementPoints: 6);

        WorldStack defenderStack = TestHelpers.SimpleStack(
            new WorldStackId("defender"),
            ai.Id,
            new WorldPosition(2, 0),
            new UnitGrid<WorldUnit>.Builder().Place(TestHelpers.SimpleUnit(new WorldUnitId("defender"), defenderDefinition), GridPosition.FrontBottom, false).Build(),
            movementPoints: 6);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithGlobals(new GlobalVariables { BattleInitiativeVariance = 0, BattleDamageVariance = 0 })
            .WithDefinition(attackerDefinition)
            .WithDefinition(defenderDefinition)
            .WithDefinition(TestHelpers.SimpleLord(human.LordId, human.Race, LordCategory.Warrior))
            .WithPlayer(human)
            .WithDefinition(TestHelpers.SimpleLord(ai.LordId, ai.Race, LordCategory.Warrior))
            .WithPlayer(ai)
            .WithStack(attackerStack)
            .WithStack(defenderStack)
            .BuildSession();

        WorldCommandResult moveResult = session.World.DispatchCommand(new WorldMoveStackCommand(attackerStack.Id, defenderStack.Position));
        Assert.That(moveResult.Error, Is.Null);

        BattleSession battle = Assert.NotNull(session.Battle);

        Assert.That(battle.IsAttackerAi, Is.False);
        Assert.That(battle.IsDefenderAi, Is.True);
        Assert.That(battle.IsAiTurn, Is.False);
    }

    [Test]
    public void AiDefenderFightTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 1);

        WorldPlayer human = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer ai = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map) with { IsAi = true };

        UnitDefinition attackerDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("attacker", maxHitpoints: 10, initiative: 50, damage: 5),
            movementPoints: 6, leadership: 6);
        UnitDefinition defenderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 10, initiative: 40, damage: 5),
            movementPoints: 6, leadership: 6);

        WorldStack attackerStack = TestHelpers.SimpleStack(
            new WorldStackId("attacker"),
            human.Id,
            new WorldPosition(0, 0),
            new UnitGrid<WorldUnit>.Builder().Place(TestHelpers.SimpleUnit(new WorldUnitId("attacker"), attackerDefinition), GridPosition.FrontBottom, false).Build(),
            movementPoints: 6);

        WorldStack defenderStack = TestHelpers.SimpleStack(
            new WorldStackId("defender"),
            ai.Id,
            new WorldPosition(2, 0),
            new UnitGrid<WorldUnit>.Builder().Place(TestHelpers.SimpleUnit(new WorldUnitId("defender"), defenderDefinition), GridPosition.FrontBottom, false).Build(),
            movementPoints: 6);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithGlobals(new GlobalVariables { BattleInitiativeVariance = 0, BattleDamageVariance = 0 })
            .WithDefinition(attackerDefinition)
            .WithDefinition(defenderDefinition)
            .WithDefinition(TestHelpers.SimpleLord(human.LordId, human.Race, LordCategory.Warrior))
            .WithPlayer(human)
            .WithDefinition(TestHelpers.SimpleLord(ai.LordId, ai.Race, LordCategory.Warrior))
            .WithPlayer(ai)
            .WithStack(attackerStack)
            .WithStack(defenderStack)
            .BuildSession();

        WorldCommandResult moveResult = session.World.DispatchCommand(new WorldMoveStackCommand(attackerStack.Id, defenderStack.Position));
        Assert.That(moveResult.Error, Is.Null);

        BattleUnitId attackerUnitId = new(0);
        BattleUnitId defenderUnitId = new(1);

        BattleSession battle = Assert.NotNull(session.Battle);

        Assert.That(battle.State.CurrentUnitId, Is.EqualTo(attackerUnitId));
        Assert.That(battle.IsAiTurn, Is.False);
        BattleCommandResult firstAttackResult = battle.DispatchCommand(new BattleAttackCommand(attackerUnitId, new BattleAttackUnitTarget(defenderUnitId)));
        Assert.That(firstAttackResult.Error, Is.Null);

        Assert.That(battle.State.GetUnit(defenderUnitId).Hitpoints, Is.EqualTo(5));
        Assert.That(battle.State.CurrentUnitId, Is.EqualTo(defenderUnitId));
        Assert.That(battle.IsAiTurn, Is.True);
        BattleCommandResult aiActionResult = battle.DispatchCommand(new BattleAiActionCommand());
        Assert.That(aiActionResult.Error, Is.Null);

        Assert.That(battle.State.GetUnit(attackerUnitId).Hitpoints, Is.EqualTo(5));
        Assert.That(battle.State.CurrentUnitId, Is.EqualTo(attackerUnitId));
        Assert.That(battle.IsAiTurn, Is.False);
        BattleCommandResult secondAttackResult = battle.DispatchCommand(new BattleAttackCommand(attackerUnitId, new BattleAttackUnitTarget(defenderUnitId)));
        Assert.That(secondAttackResult.Error, Is.Null);

        Assert.That(battle.State.Outcome, Is.EqualTo(new BattleOutcome(BattleSide.Attacker)));
    }
}
