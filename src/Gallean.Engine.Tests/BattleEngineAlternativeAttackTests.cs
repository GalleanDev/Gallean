namespace Gallean.Engine.Tests;

public sealed class BattleEngineAlternativeAttackTests
{
    private static UnitAttackDefinition SlashAttack(UnitAttackReach reach)
    {
        return TestHelpers.SimpleAttack(new UnitAttackDefinitionId("slash"), initiative: 50, damage: 30, accuracy: 100, reach: reach);
    }

    private static UnitDefinition DoppelgangerDefinition(UnitAttackDefinitionId? alternativeAttackId)
    {
        return TestHelpers.SimpleUnitDefinition("doppelganger", maxHitpoints: 60, initiative: 50, damage: 5) with
        {
            Level = 3,
            Attack = TestHelpers.SimpleAttack(
                    new UnitAttackDefinitionId("doppelganger"),
                    initiative: 50,
                    damage: 5,
                    accuracy: 100,
                    reach: UnitAttackReach.Any) with
                {
                    Type = UnitAttackType.Doppelganger,
                    IsInfinite = true,
                    AlternativeAttackId = alternativeAttackId
                }
        };
    }

    [Test]
    public void AttacksWithAlternativeTest()
    {
        UnitDefinition doppelgangerDefinition = DoppelgangerDefinition(new UnitAttackDefinitionId("slash"));
        UnitDefinition guardianDefinition = TestHelpers.SimpleUnitDefinition("guardian", maxHitpoints: 200, initiative: 20, damage: 30) with
        {
            Category = UnitCategory.Guardian
        };

        BattleUnit doppelganger = new(new BattleUnitId(0), null, doppelgangerDefinition.Id, BattleSide.Attacker, 60, 0, 0, 3, [], [], null, [], null, false);
        BattleUnit guardian = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, guardianDefinition.Id, BattleSide.Defender, hitpoints: 200);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(doppelgangerDefinition)
            .WithDefinition(guardianDefinition)
            .WithDefinition(SlashAttack(UnitAttackReach.Adjacent))
            .WithAttacker(doppelganger, GridPosition.FrontBottom)
            .WithDefender(guardian, GridPosition.FrontBottom)
            .Build();

        BattleGetAttackInfoCommand.Result result = engine.CheckCommand(new BattleGetAttackInfoCommand(doppelganger.Id), []);

        Assert.That(result, Is.EqualTo(new BattleGetAttackInfoCommand.Result(
        [
            new BattleGetAttackInfoCommand.AttackInfo(new BattleAttackUnitTarget(doppelganger.Id), [], BattleCommandError.DoppelgangerTargetInvalid),
            new BattleGetAttackInfoCommand.AttackInfo(new BattleAttackUnitTarget(guardian.Id), [new BattleAttackUnitTarget(guardian.Id)], null),
            new BattleGetAttackInfoCommand.AttackInfo(new BattleAttackTileTarget(BattleSide.Attacker, GridPosition.FrontCenter), [], BattleCommandError.AttackRequiresUnitTarget),
            new BattleGetAttackInfoCommand.AttackInfo(new BattleAttackTileTarget(BattleSide.Attacker, GridPosition.FrontTop), [], BattleCommandError.AttackRequiresUnitTarget),
            new BattleGetAttackInfoCommand.AttackInfo(new BattleAttackTileTarget(BattleSide.Attacker, GridPosition.BackBottom), [], BattleCommandError.AttackRequiresUnitTarget),
            new BattleGetAttackInfoCommand.AttackInfo(new BattleAttackTileTarget(BattleSide.Attacker, GridPosition.BackCenter), [], BattleCommandError.AttackRequiresUnitTarget),
            new BattleGetAttackInfoCommand.AttackInfo(new BattleAttackTileTarget(BattleSide.Attacker, GridPosition.BackTop), [], BattleCommandError.AttackRequiresUnitTarget),
            new BattleGetAttackInfoCommand.AttackInfo(new BattleAttackTileTarget(BattleSide.Defender, GridPosition.FrontCenter), [], BattleCommandError.AttackRequiresUnitTarget),
            new BattleGetAttackInfoCommand.AttackInfo(new BattleAttackTileTarget(BattleSide.Defender, GridPosition.FrontTop), [], BattleCommandError.AttackRequiresUnitTarget),
            new BattleGetAttackInfoCommand.AttackInfo(new BattleAttackTileTarget(BattleSide.Defender, GridPosition.BackBottom), [], BattleCommandError.AttackRequiresUnitTarget),
            new BattleGetAttackInfoCommand.AttackInfo(new BattleAttackTileTarget(BattleSide.Defender, GridPosition.BackCenter), [], BattleCommandError.AttackRequiresUnitTarget),
            new BattleGetAttackInfoCommand.AttackInfo(new BattleAttackTileTarget(BattleSide.Defender, GridPosition.BackTop), [], BattleCommandError.AttackRequiresUnitTarget)
        ])));

        engine.CheckCommand(new BattleAttackCommand(doppelganger.Id, new BattleAttackUnitTarget(guardian.Id)),
        [
            new BattleUnitAttackEvent(doppelganger.Id, new BattleAttackUnitTarget(guardian.Id), [new UnitAttackDefinitionId("slash")]),
            new BattleUnitAttackedEvent(doppelganger.Id, guardian.Id, new UnitAttackDefinitionId("slash")),
            new BattleUnitAttackHitEvent(doppelganger.Id, guardian.Id, new UnitAttackDefinitionId("slash"), 30),
            new BattleUnitTurnEndingEvent(doppelganger.Id),
            new BattleUnitTurnEndedEvent(doppelganger.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(guardian.Id, BattleUnitTurnType.Normal))
        ]);
    }

    [Test]
    public void TransformTargetBothAttacksTest()
    {
        UnitDefinition doppelgangerDefinition = DoppelgangerDefinition(new UnitAttackDefinitionId("slash"));
        UnitDefinition knightDefinition = TestHelpers.SimpleUnitDefinition("knight", maxHitpoints: 120, initiative: 40, damage: 25) with
        {
            Level = 2
        };

        BattleUnit doppelganger = new(new BattleUnitId(0), null, doppelgangerDefinition.Id, BattleSide.Attacker, 60, 0, 0, 3, [], [], null, [], null, false);
        BattleUnit knight = new(new BattleUnitId(1), null, knightDefinition.Id, BattleSide.Defender, 120, 0, 0, 2, [], [], null, [], null, false);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(doppelgangerDefinition)
            .WithDefinition(knightDefinition)
            .WithDefinition(SlashAttack(UnitAttackReach.Adjacent))
            .WithAttacker(doppelganger, GridPosition.FrontBottom)
            .WithDefender(knight, GridPosition.FrontBottom)
            .Build();

        engine.CheckCommand(new BattleAttackCommand(doppelganger.Id, new BattleAttackUnitTarget(knight.Id)),
        [
            new BattleUnitAttackEvent(doppelganger.Id, new BattleAttackUnitTarget(knight.Id), [new UnitAttackDefinitionId("doppelganger")]),
            new BattleUnitTransformAppliedEvent(doppelganger.Id, doppelganger.Id, BattleTransformType.Doppelganger, knightDefinition.Id, 2, 120, null),
            new BattleUnitTurnEndingEvent(doppelganger.Id),
            new BattleUnitTurnEndedEvent(doppelganger.Id),
            new BattleRoundEndedEvent(),
            new BattleRoundStartedEvent(BattleRound.Normal(1), [new BattleUnitTurn(doppelganger.Id, BattleUnitTurnType.Normal), new BattleUnitTurn(knight.Id, BattleUnitTurnType.Normal)]),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(doppelganger.Id, BattleUnitTurnType.Normal))
        ]);
    }

    [Test]
    public void NoAlternativeAttackTest()
    {
        UnitDefinition doppelgangerDefinition = DoppelgangerDefinition(null);
        UnitDefinition guardianDefinition = TestHelpers.SimpleUnitDefinition("guardian", maxHitpoints: 200, initiative: 20, damage: 30) with
        {
            Category = UnitCategory.Guardian
        };

        BattleUnit doppelganger = new(new BattleUnitId(0), null, doppelgangerDefinition.Id, BattleSide.Attacker, 60, 0, 0, 3, [], [], null, [], null, false);
        BattleUnit guardian = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, guardianDefinition.Id, BattleSide.Defender, hitpoints: 200);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(doppelgangerDefinition)
            .WithDefinition(guardianDefinition)
            .WithAttacker(doppelganger, GridPosition.FrontBottom)
            .WithDefender(guardian, GridPosition.FrontBottom)
            .Build();

        engine.CheckCommandError(
            new BattleAttackCommand(doppelganger.Id, new BattleAttackUnitTarget(guardian.Id)),
            BattleCommandError.DoppelgangerTargetInvalid);
    }

    [Test]
    public void GetAttackInfoTest()
    {
        UnitDefinition doppelgangerDefinition = DoppelgangerDefinition(new UnitAttackDefinitionId("slash"));
        UnitDefinition knightDefinition = TestHelpers.SimpleUnitDefinition("knight", maxHitpoints: 120, initiative: 40, damage: 25) with
        {
            Level = 2
        };
        UnitDefinition archerDefinition = TestHelpers.SimpleUnitDefinition("archer", maxHitpoints: 50, initiative: 30, damage: 10);

        BattleUnit doppelganger = new(new BattleUnitId(0), null, doppelgangerDefinition.Id, BattleSide.Attacker, 60, 0, 0, 3, [], [], null, [], null, false);
        BattleUnit knight = new(new BattleUnitId(1), null, knightDefinition.Id, BattleSide.Defender, 120, 0, 0, 2, [], [], null, [], null, false);
        BattleUnit archer = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, archerDefinition.Id, BattleSide.Defender, hitpoints: 50);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(doppelgangerDefinition)
            .WithDefinition(knightDefinition)
            .WithDefinition(archerDefinition)
            .WithDefinition(SlashAttack(UnitAttackReach.All))
            .WithAttacker(doppelganger, GridPosition.FrontBottom)
            .WithDefender(knight, GridPosition.FrontBottom)
            .WithDefender(archer, GridPosition.FrontTop)
            .Build();

        BattleGetAttackInfoCommand.Result result = engine.CheckCommand(new BattleGetAttackInfoCommand(doppelganger.Id), []);

        Assert.That(result, Is.EqualTo(new BattleGetAttackInfoCommand.Result(
        [
            new BattleGetAttackInfoCommand.AttackInfo(new BattleAttackUnitTarget(doppelganger.Id), [], BattleCommandError.DoppelgangerTargetInvalid),
            new BattleGetAttackInfoCommand.AttackInfo(new BattleAttackUnitTarget(knight.Id), [new BattleAttackUnitTarget(knight.Id)], null),
            new BattleGetAttackInfoCommand.AttackInfo(new BattleAttackUnitTarget(archer.Id), [new BattleAttackUnitTarget(archer.Id)], null),
            new BattleGetAttackInfoCommand.AttackInfo(new BattleAttackTileTarget(BattleSide.Attacker, GridPosition.FrontCenter), [], BattleCommandError.AttackRequiresUnitTarget),
            new BattleGetAttackInfoCommand.AttackInfo(new BattleAttackTileTarget(BattleSide.Attacker, GridPosition.FrontTop), [], BattleCommandError.AttackRequiresUnitTarget),
            new BattleGetAttackInfoCommand.AttackInfo(new BattleAttackTileTarget(BattleSide.Attacker, GridPosition.BackBottom), [], BattleCommandError.AttackRequiresUnitTarget),
            new BattleGetAttackInfoCommand.AttackInfo(new BattleAttackTileTarget(BattleSide.Attacker, GridPosition.BackCenter), [], BattleCommandError.AttackRequiresUnitTarget),
            new BattleGetAttackInfoCommand.AttackInfo(new BattleAttackTileTarget(BattleSide.Attacker, GridPosition.BackTop), [], BattleCommandError.AttackRequiresUnitTarget),
            new BattleGetAttackInfoCommand.AttackInfo(new BattleAttackTileTarget(BattleSide.Defender, GridPosition.FrontCenter), [], BattleCommandError.AttackRequiresUnitTarget),
            new BattleGetAttackInfoCommand.AttackInfo(new BattleAttackTileTarget(BattleSide.Defender, GridPosition.BackBottom), [], BattleCommandError.AttackRequiresUnitTarget),
            new BattleGetAttackInfoCommand.AttackInfo(new BattleAttackTileTarget(BattleSide.Defender, GridPosition.BackCenter), [], BattleCommandError.AttackRequiresUnitTarget),
            new BattleGetAttackInfoCommand.AttackInfo(new BattleAttackTileTarget(BattleSide.Defender, GridPosition.BackTop), [], BattleCommandError.AttackRequiresUnitTarget)
        ])));
    }

    [Test]
    public void TransformTargetAvailableTest()
    {
        UnitDefinition doppelgangerDefinition = DoppelgangerDefinition(new UnitAttackDefinitionId("slash"));
        UnitDefinition knightDefinition = TestHelpers.SimpleUnitDefinition("knight", maxHitpoints: 120, initiative: 40, damage: 25) with
        {
            Level = 2
        };
        UnitDefinition guardianDefinition = TestHelpers.SimpleUnitDefinition("guardian", maxHitpoints: 200, initiative: 20, damage: 30) with
        {
            Category = UnitCategory.Guardian
        };

        BattleUnit doppelganger = new(new BattleUnitId(0), null, doppelgangerDefinition.Id, BattleSide.Attacker, 60, 0, 0, 3, [], [], null, [], null, false);
        BattleUnit knight = new(new BattleUnitId(1), null, knightDefinition.Id, BattleSide.Defender, 120, 0, 0, 2, [], [], null, [], null, false);
        BattleUnit guardian = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, guardianDefinition.Id, BattleSide.Defender, hitpoints: 200);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(doppelgangerDefinition)
            .WithDefinition(knightDefinition)
            .WithDefinition(guardianDefinition)
            .WithDefinition(SlashAttack(UnitAttackReach.Adjacent))
            .WithAttacker(doppelganger, GridPosition.FrontBottom)
            .WithDefender(knight, GridPosition.FrontBottom)
            .WithDefender(guardian, GridPosition.FrontTop)
            .Build();

        BattleGetAttackInfoCommand.Result result = engine.CheckCommand(new BattleGetAttackInfoCommand(doppelganger.Id), []);

        Assert.That(result, Is.EqualTo(new BattleGetAttackInfoCommand.Result(
        [
            new BattleGetAttackInfoCommand.AttackInfo(new BattleAttackUnitTarget(doppelganger.Id), [], BattleCommandError.DoppelgangerTargetInvalid),
            new BattleGetAttackInfoCommand.AttackInfo(new BattleAttackUnitTarget(knight.Id), [new BattleAttackUnitTarget(knight.Id)], null),
            new BattleGetAttackInfoCommand.AttackInfo(new BattleAttackUnitTarget(guardian.Id), [], BattleCommandError.DoppelgangerTargetInvalid),
            new BattleGetAttackInfoCommand.AttackInfo(new BattleAttackTileTarget(BattleSide.Attacker, GridPosition.FrontCenter), [], BattleCommandError.AttackRequiresUnitTarget),
            new BattleGetAttackInfoCommand.AttackInfo(new BattleAttackTileTarget(BattleSide.Attacker, GridPosition.FrontTop), [], BattleCommandError.AttackRequiresUnitTarget),
            new BattleGetAttackInfoCommand.AttackInfo(new BattleAttackTileTarget(BattleSide.Attacker, GridPosition.BackBottom), [], BattleCommandError.AttackRequiresUnitTarget),
            new BattleGetAttackInfoCommand.AttackInfo(new BattleAttackTileTarget(BattleSide.Attacker, GridPosition.BackCenter), [], BattleCommandError.AttackRequiresUnitTarget),
            new BattleGetAttackInfoCommand.AttackInfo(new BattleAttackTileTarget(BattleSide.Attacker, GridPosition.BackTop), [], BattleCommandError.AttackRequiresUnitTarget),
            new BattleGetAttackInfoCommand.AttackInfo(new BattleAttackTileTarget(BattleSide.Defender, GridPosition.FrontCenter), [], BattleCommandError.AttackRequiresUnitTarget),
            new BattleGetAttackInfoCommand.AttackInfo(new BattleAttackTileTarget(BattleSide.Defender, GridPosition.BackBottom), [], BattleCommandError.AttackRequiresUnitTarget),
            new BattleGetAttackInfoCommand.AttackInfo(new BattleAttackTileTarget(BattleSide.Defender, GridPosition.BackCenter), [], BattleCommandError.AttackRequiresUnitTarget),
            new BattleGetAttackInfoCommand.AttackInfo(new BattleAttackTileTarget(BattleSide.Defender, GridPosition.BackTop), [], BattleCommandError.AttackRequiresUnitTarget)
        ])));

        engine.CheckCommandError(
            new BattleAttackCommand(doppelganger.Id, new BattleAttackUnitTarget(guardian.Id)),
            BattleCommandError.DoppelgangerTargetInvalid);
    }

    [Test]
    public void DeadTransformTargetTest()
    {
        UnitDefinition doppelgangerDefinition = DoppelgangerDefinition(new UnitAttackDefinitionId("slash"));
        UnitDefinition knightDefinition = TestHelpers.SimpleUnitDefinition("knight", maxHitpoints: 120, initiative: 40, damage: 25) with
        {
            Level = 2
        };
        UnitDefinition guardianDefinition = TestHelpers.SimpleUnitDefinition("guardian", maxHitpoints: 200, initiative: 20, damage: 30) with
        {
            Category = UnitCategory.Guardian
        };

        BattleUnit doppelganger = new(new BattleUnitId(0), null, doppelgangerDefinition.Id, BattleSide.Attacker, 60, 0, 0, 3, [], [], null, [], null, false);
        BattleUnit knight = new(new BattleUnitId(1), null, knightDefinition.Id, BattleSide.Defender, 0, 0, 0, 2, [], [], null, [], null, false);
        BattleUnit guardian = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, guardianDefinition.Id, BattleSide.Defender, hitpoints: 200);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(doppelgangerDefinition)
            .WithDefinition(knightDefinition)
            .WithDefinition(guardianDefinition)
            .WithDefinition(SlashAttack(UnitAttackReach.Adjacent))
            .WithAttacker(doppelganger, GridPosition.FrontBottom)
            .WithDefender(knight, GridPosition.FrontBottom)
            .WithDefender(guardian, GridPosition.FrontTop)
            .Build();

        engine.CheckCommand(new BattleAttackCommand(doppelganger.Id, new BattleAttackUnitTarget(guardian.Id)),
        [
            new BattleUnitAttackEvent(doppelganger.Id, new BattleAttackUnitTarget(guardian.Id), [new UnitAttackDefinitionId("slash")]),
            new BattleUnitAttackedEvent(doppelganger.Id, guardian.Id, new UnitAttackDefinitionId("slash")),
            new BattleUnitAttackHitEvent(doppelganger.Id, guardian.Id, new UnitAttackDefinitionId("slash"), 30),
            new BattleUnitTurnEndingEvent(doppelganger.Id),
            new BattleUnitTurnEndedEvent(doppelganger.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(guardian.Id, BattleUnitTurnType.Normal))
        ]);
    }

    [Test]
    public void AiTransformPossibleTest()
    {
        UnitDefinition doppelgangerDefinition = DoppelgangerDefinition(new UnitAttackDefinitionId("slash"));
        UnitDefinition knightDefinition = TestHelpers.SimpleUnitDefinition("knight", maxHitpoints: 120, initiative: 40, damage: 25) with
        {
            Level = 2
        };
        UnitDefinition guardianDefinition = TestHelpers.SimpleUnitDefinition("guardian", maxHitpoints: 200, initiative: 20, damage: 30) with
        {
            Category = UnitCategory.Guardian
        };

        BattleUnit doppelganger = new(new BattleUnitId(0), null, doppelgangerDefinition.Id, BattleSide.Attacker, 60, 0, 0, 3, [], [], null, [], null, false);
        BattleUnit knight = new(new BattleUnitId(1), null, knightDefinition.Id, BattleSide.Defender, 120, 0, 0, 2, [], [], null, [], null, false);
        BattleUnit guardian = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, guardianDefinition.Id, BattleSide.Defender, hitpoints: 200);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(doppelgangerDefinition)
            .WithDefinition(knightDefinition)
            .WithDefinition(guardianDefinition)
            .WithDefinition(SlashAttack(UnitAttackReach.Adjacent))
            .WithAttacker(doppelganger, GridPosition.FrontBottom)
            .WithDefender(knight, GridPosition.FrontBottom)
            .WithDefender(guardian, GridPosition.FrontTop)
            .Build();

        engine.CheckCommand(new BattleAiActionCommand(),
        [
            new BattleUnitAttackEvent(doppelganger.Id, new BattleAttackUnitTarget(knight.Id), [new UnitAttackDefinitionId("doppelganger")]),
            new BattleUnitTransformAppliedEvent(doppelganger.Id, doppelganger.Id, BattleTransformType.Doppelganger, knightDefinition.Id, 2, 120, null),
            new BattleUnitTurnEndingEvent(doppelganger.Id),
            new BattleUnitTurnEndedEvent(doppelganger.Id),
            new BattleRoundEndedEvent(),
            new BattleRoundStartedEvent(BattleRound.Normal(1), [new BattleUnitTurn(doppelganger.Id, BattleUnitTurnType.Normal), new BattleUnitTurn(knight.Id, BattleUnitTurnType.Normal), new BattleUnitTurn(guardian.Id, BattleUnitTurnType.Normal)]),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(doppelganger.Id, BattleUnitTurnType.Normal))
        ]);
    }

    [Test]
    public void AiTransformImpossibleTest()
    {
        UnitDefinition doppelgangerDefinition = DoppelgangerDefinition(new UnitAttackDefinitionId("slash"));
        UnitDefinition guardianDefinition = TestHelpers.SimpleUnitDefinition("guardian", maxHitpoints: 200, initiative: 20, damage: 30) with
        {
            Category = UnitCategory.Guardian
        };

        BattleUnit doppelganger = new(new BattleUnitId(0), null, doppelgangerDefinition.Id, BattleSide.Attacker, 60, 0, 0, 3, [], [], null, [], null, false);
        BattleUnit guardian = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, guardianDefinition.Id, BattleSide.Defender, hitpoints: 200);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(doppelgangerDefinition)
            .WithDefinition(guardianDefinition)
            .WithDefinition(SlashAttack(UnitAttackReach.Adjacent))
            .WithAttacker(doppelganger, GridPosition.FrontBottom)
            .WithDefender(guardian, GridPosition.FrontBottom)
            .Build();

        engine.CheckCommand(new BattleAiActionCommand(),
        [
            new BattleUnitAttackEvent(doppelganger.Id, new BattleAttackUnitTarget(guardian.Id), [new UnitAttackDefinitionId("slash")]),
            new BattleUnitAttackedEvent(doppelganger.Id, guardian.Id, new UnitAttackDefinitionId("slash")),
            new BattleUnitAttackHitEvent(doppelganger.Id, guardian.Id, new UnitAttackDefinitionId("slash"), 30),
            new BattleUnitTurnEndingEvent(doppelganger.Id),
            new BattleUnitTurnEndedEvent(doppelganger.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(guardian.Id, BattleUnitTurnType.Normal))
        ]);
    }

    [Test]
    public void ModifierScopeTest()
    {
        UnitDefinition doppelgangerDefinition = DoppelgangerDefinition(new UnitAttackDefinitionId("slash"));
        UnitDefinition guardianDefinition = TestHelpers.SimpleUnitDefinition("guardian", maxHitpoints: 200, initiative: 20, damage: 30) with
        {
            Category = UnitCategory.Guardian
        };

        UnitModifierDefinition weaponModifier = TestHelpers.SimpleModifier("weapon") with
        {
            Damage = new UnitStatModifier(0, 50)
        };

        BattleUnit doppelganger = new(new BattleUnitId(0), null, doppelgangerDefinition.Id, BattleSide.Attacker, 60, 0, 0, 3, [UnitModifier.Permanent(weaponModifier.Id)], [], null, [], null, false);
        BattleUnit guardian = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, guardianDefinition.Id, BattleSide.Defender, hitpoints: 200);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(doppelgangerDefinition)
            .WithDefinition(guardianDefinition)
            .WithDefinition(weaponModifier)
            .WithDefinition(SlashAttack(UnitAttackReach.Adjacent))
            .WithAttacker(doppelganger, GridPosition.FrontBottom)
            .WithDefender(guardian, GridPosition.FrontBottom)
            .Build();

        engine.CheckCommand(new BattleAttackCommand(doppelganger.Id, new BattleAttackUnitTarget(guardian.Id)),
        [
            new BattleUnitAttackEvent(doppelganger.Id, new BattleAttackUnitTarget(guardian.Id), [new UnitAttackDefinitionId("slash")]),
            new BattleUnitAttackedEvent(doppelganger.Id, guardian.Id, new UnitAttackDefinitionId("slash")),
            new BattleUnitAttackHitEvent(doppelganger.Id, guardian.Id, new UnitAttackDefinitionId("slash"), 45),
            new BattleUnitTurnEndingEvent(doppelganger.Id),
            new BattleUnitTurnEndedEvent(doppelganger.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(guardian.Id, BattleUnitTurnType.Normal))
        ]);
    }
}
