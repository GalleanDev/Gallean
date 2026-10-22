namespace Gallean.Engine.Tests;

public sealed class WorldBattleModifierCalculatorTests
{
    [Test]
    public void HumanEasyTest()
    {
        WorldMap map = TestHelpers.SimpleMap(1, 1);
        WorldPlayer human = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        (WorldEngine engine, IEngineContext context) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDifficulty(WorldDifficulty.Easy)
            .WithSuggestedLevel(15)
            .WithDefinition(TestHelpers.SimpleLord(human.LordId, human.Race, LordCategory.Warrior))
            .WithPlayer(human)
            .BuildEngineWithContext();

        Assert.That(
            WorldBattleModifierCalculator.GetModifiers(engine.State, human.Id, context),
            Is.EqualTo(new BattleSideModifiers(ExperiencePercent: 20, AccuracyBonus: 0, RandomDamageBonus: 6)));
    }

    [Test]
    public void HumanAverageTest()
    {
        WorldMap map = TestHelpers.SimpleMap(1, 1);
        WorldPlayer human = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        (WorldEngine engine, IEngineContext context) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDifficulty(WorldDifficulty.Average)
            .WithSuggestedLevel(15)
            .WithDefinition(TestHelpers.SimpleLord(human.LordId, human.Race, LordCategory.Warrior))
            .WithPlayer(human)
            .BuildEngineWithContext();

        Assert.That(
            WorldBattleModifierCalculator.GetModifiers(engine.State, human.Id, context),
            Is.EqualTo(BattleSideModifiers.None));
    }

    [Test]
    public void HumanHardTest()
    {
        WorldMap map = TestHelpers.SimpleMap(1, 1);
        WorldPlayer human = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        (WorldEngine engine, IEngineContext context) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDifficulty(WorldDifficulty.Hard)
            .WithSuggestedLevel(15)
            .WithDefinition(TestHelpers.SimpleLord(human.LordId, human.Race, LordCategory.Warrior))
            .WithPlayer(human)
            .BuildEngineWithContext();

        Assert.That(
            WorldBattleModifierCalculator.GetModifiers(engine.State, human.Id, context),
            Is.EqualTo(BattleSideModifiers.None));
    }

    [Test]
    public void HumanVeryHardTest()
    {
        WorldMap map = TestHelpers.SimpleMap(1, 1);
        WorldPlayer human = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        (WorldEngine engine, IEngineContext context) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDifficulty(WorldDifficulty.VeryHard)
            .WithSuggestedLevel(15)
            .WithDefinition(TestHelpers.SimpleLord(human.LordId, human.Race, LordCategory.Warrior))
            .WithPlayer(human)
            .BuildEngineWithContext();

        Assert.That(
            WorldBattleModifierCalculator.GetModifiers(engine.State, human.Id, context),
            Is.EqualTo(BattleSideModifiers.None));
    }

    [Test]
    public void AiEasyTest()
    {
        WorldMap map = TestHelpers.SimpleMap(1, 1);
        WorldPlayer ai = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with { IsAi = true };

        (WorldEngine engine, IEngineContext context) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDifficulty(WorldDifficulty.Easy)
            .WithSuggestedLevel(10)
            .WithDefinition(TestHelpers.SimpleLord(ai.LordId, ai.Race, LordCategory.Warrior))
            .WithPlayer(ai)
            .BuildEngineWithContext();

        Assert.That(
            WorldBattleModifierCalculator.GetModifiers(engine.State, ai.Id, context),
            Is.EqualTo(new BattleSideModifiers(ExperiencePercent: 0, AccuracyBonus: -15, RandomDamageBonus: 0)));
    }

    [Test]
    public void AiAverageTest()
    {
        WorldMap map = TestHelpers.SimpleMap(1, 1);
        WorldPlayer ai = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with { IsAi = true };

        (WorldEngine engine, IEngineContext context) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDifficulty(WorldDifficulty.Average)
            .WithSuggestedLevel(10)
            .WithDefinition(TestHelpers.SimpleLord(ai.LordId, ai.Race, LordCategory.Warrior))
            .WithPlayer(ai)
            .BuildEngineWithContext();

        Assert.That(
            WorldBattleModifierCalculator.GetModifiers(engine.State, ai.Id, context),
            Is.EqualTo(new BattleSideModifiers(ExperiencePercent: 25, AccuracyBonus: 0, RandomDamageBonus: 0)));
    }

    [Test]
    public void AiHardTest()
    {
        WorldMap map = TestHelpers.SimpleMap(1, 1);
        WorldPlayer ai = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with { IsAi = true };

        (WorldEngine engine, IEngineContext context) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDifficulty(WorldDifficulty.Hard)
            .WithSuggestedLevel(10)
            .WithDefinition(TestHelpers.SimpleLord(ai.LordId, ai.Race, LordCategory.Warrior))
            .WithPlayer(ai)
            .BuildEngineWithContext();

        Assert.That(
            WorldBattleModifierCalculator.GetModifiers(engine.State, ai.Id, context),
            Is.EqualTo(new BattleSideModifiers(ExperiencePercent: 50, AccuracyBonus: 5, RandomDamageBonus: 0)));
    }

    [Test]
    public void AiVeryHardTest()
    {
        WorldMap map = TestHelpers.SimpleMap(1, 1);
        WorldPlayer ai = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with { IsAi = true };

        (WorldEngine engine, IEngineContext context) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDifficulty(WorldDifficulty.VeryHard)
            .WithSuggestedLevel(10)
            .WithDefinition(TestHelpers.SimpleLord(ai.LordId, ai.Race, LordCategory.Warrior))
            .WithPlayer(ai)
            .BuildEngineWithContext();

        Assert.That(
            WorldBattleModifierCalculator.GetModifiers(engine.State, ai.Id, context),
            Is.EqualTo(new BattleSideModifiers(ExperiencePercent: 100, AccuracyBonus: 10, RandomDamageBonus: 0)));
    }

    [Test]
    public void AiLowSuggestedLevelTest()
    {
        WorldMap map = TestHelpers.SimpleMap(1, 1);
        WorldPlayer ai = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with { IsAi = true };

        (WorldEngine engine, IEngineContext context) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDifficulty(WorldDifficulty.VeryHard)
            .WithSuggestedLevel(9)
            .WithDefinition(TestHelpers.SimpleLord(ai.LordId, ai.Race, LordCategory.Warrior))
            .WithPlayer(ai)
            .BuildEngineWithContext();

        Assert.That(
            WorldBattleModifierCalculator.GetModifiers(engine.State, ai.Id, context),
            Is.EqualTo(new BattleSideModifiers(ExperiencePercent: 0, AccuracyBonus: 10, RandomDamageBonus: 0)));
    }

    [Test]
    public void NeutralOwnerTest()
    {
        WorldMap map = TestHelpers.SimpleMap(1, 1);
        WorldPlayer neutral = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Neutral, map) with { IsAi = true };

        (WorldEngine engine, IEngineContext context) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDifficulty(WorldDifficulty.VeryHard)
            .WithSuggestedLevel(10)
            .WithDefinition(TestHelpers.SimpleLord(neutral.LordId, neutral.Race, LordCategory.Warrior))
            .WithPlayer(neutral)
            .BuildEngineWithContext();

        Assert.That(
            WorldBattleModifierCalculator.GetModifiers(engine.State, neutral.Id, context),
            Is.EqualTo(new BattleSideModifiers(ExperiencePercent: 0, AccuracyBonus: 10, RandomDamageBonus: 0)));
    }

    [Test]
    public void NoOwnerTest()
    {
        WorldMap map = TestHelpers.SimpleMap(1, 1);
        WorldPlayer human = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        (WorldEngine engine, IEngineContext context) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDifficulty(WorldDifficulty.Easy)
            .WithSuggestedLevel(15)
            .WithDefinition(TestHelpers.SimpleLord(human.LordId, human.Race, LordCategory.Warrior))
            .WithPlayer(human)
            .BuildEngineWithContext();

        Assert.That(
            WorldBattleModifierCalculator.GetModifiers(engine.State, ownerId: null, context),
            Is.EqualTo(new BattleSideModifiers(ExperiencePercent: 0, AccuracyBonus: -15, RandomDamageBonus: 0)));
    }
}
