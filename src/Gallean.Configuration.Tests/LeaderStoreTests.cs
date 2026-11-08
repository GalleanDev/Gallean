using Gallean.Engine;
using Gallean.Engine.Contracts;
using Gallean.Tests.Common;

namespace Gallean.Configuration.Tests;

public sealed class LeaderStoreTests
{
    private static readonly ScenarioLeader Uther = new(
        new WorldUnit(
            new WorldUnitId("S107UN0001"),
            new UnitDefinitionId("G000UU0019"),
            Hitpoints: 180,
            Experience: 120,
            Level: 3,
            [UnitModifier.Permanent(new UnitModifierDefinitionId("G000UM4008"))],
            Name: new LiteralUnitName("Uther"),
            IsTypeLocked: false),
        LeaderEquipment.Empty.With(
            LeaderEquipmentSlot.Banner,
            new WorldItem(new WorldItemId("Exported1"), new ItemDefinitionId("G000IG1001"), Charges: null)),
        WorldInventory.Empty,
        PendingLeaderUpgrades: 1,
        BattlesWon: 7);

    private static readonly ScenarioLeader Elena = new(
        new WorldUnit(
            new WorldUnitId("S108UN0001"),
            new UnitDefinitionId("G000UU0021"),
            Hitpoints: 65,
            Experience: 0,
            Level: 1,
            [],
            Name: null,
            IsTypeLocked: false),
        LeaderEquipment.Empty,
        WorldInventory.Empty,
        PendingLeaderUpgrades: 0,
        BattlesWon: 0);

    [Test]
    public async Task SaveEnumerateTest()
    {
        using Sandbox sb = Sandbox.Create();

        LeaderStore store = new(sb.GetPath("leaders"));

        Assert.That(await store.EnumerateAsync(), Is.Empty);

        LeaderEntry first = await store.SaveAsync(Uther);
        LeaderEntry second = await store.SaveAsync(Elena);

        Assert.That(first, Is.EqualTo(new LeaderEntry(new LeaderId(1), Uther)));
        Assert.That(second, Is.EqualTo(new LeaderEntry(new LeaderId(2), Elena)));
        Assert.That(await store.EnumerateAsync(), Is.EqualTo([
            new LeaderEntry(new LeaderId(1), Uther),
            new LeaderEntry(new LeaderId(2), Elena)
        ]));
        Assert.That(sb.GetTree(), Is.EquivalentTo(["leaders/", "leaders/leader-1.json", "leaders/leader-2.json"]));
    }

    [Test]
    public async Task DeleteTest()
    {
        using Sandbox sb = Sandbox.Create();

        LeaderStore store = new(sb.RootPath);

        await store.SaveAsync(Uther);
        await store.SaveAsync(Elena);
        await store.DeleteAsync(new LeaderId(1));

        Assert.That(await store.EnumerateAsync(), Is.EqualTo([
            new LeaderEntry(new LeaderId(2), Elena)
        ]));

        LeaderEntry entry = await store.SaveAsync(Uther);

        Assert.That(entry.Id, Is.EqualTo(new LeaderId(3)));
    }

    [Test]
    public async Task FormatTest()
    {
        using Sandbox sb = Sandbox.Create();

        File.WriteAllLines(sb.GetPath("leader-4.json"), [
            "{",
            "  \"formatVersion\": 1,",
            "  \"leader\": {",
            "    \"unit\": {",
            "      \"id\": \"S107UN0001\",",
            "      \"definitionId\": \"G000UU0019\",",
            "      \"hitpoints\": 180,",
            "      \"experience\": 120,",
            "      \"level\": 3,",
            "      \"modifiers\": [",
            "        {",
            "          \"definitionId\": \"G000UM4008\",",
            "          \"remainingTurns\": null,",
            "          \"source\": \"Unit\"",
            "        }",
            "      ],",
            "      \"name\": \"Uther\",",
            "      \"isTypeLocked\": false",
            "    },",
            "    \"equipment\": {",
            "      \"items\": {",
            "        \"Banner\": {",
            "          \"id\": \"Exported1\",",
            "          \"definitionId\": \"G000IG1001\",",
            "          \"charges\": null",
            "        }",
            "      }",
            "    },",
            "    \"inventory\": [],",
            "    \"pendingLeaderUpgrades\": 1,",
            "    \"battlesWon\": 7",
            "  }",
            "}"
        ]);

        LeaderStore store = new(sb.RootPath);

        Assert.That(await store.EnumerateAsync(), Is.EqualTo([
            new LeaderEntry(new LeaderId(4), Uther)
        ]));
    }

    [Test]
    public async Task SaveFormatTest()
    {
        using Sandbox sb = Sandbox.Create();

        LeaderStore store = new(sb.RootPath);

        await store.SaveAsync(Elena);

        Assert.That(File.ReadAllLines(sb.GetPath("leader-1.json")), Is.EqualTo([
            "{",
            "  \"formatVersion\": 1,",
            "  \"leader\": {",
            "    \"unit\": {",
            "      \"id\": \"S108UN0001\",",
            "      \"definitionId\": \"G000UU0021\",",
            "      \"hitpoints\": 65,",
            "      \"experience\": 0,",
            "      \"level\": 1,",
            "      \"modifiers\": [],",
            "      \"name\": null,",
            "      \"isTypeLocked\": false",
            "    },",
            "    \"equipment\": {",
            "      \"items\": {}",
            "    },",
            "    \"inventory\": [],",
            "    \"pendingLeaderUpgrades\": 0,",
            "    \"battlesWon\": 0",
            "  }",
            "}"
        ]));
    }

    [Test]
    public async Task UnreadableFilesTest()
    {
        using Sandbox sb = Sandbox.Create();

        LeaderStore store = new(sb.RootPath);

        await store.SaveAsync(Uther);

        File.WriteAllText(sb.GetPath("leader-2.json"), "not a leader");
        File.WriteAllText(sb.GetPath("leader-3.json"), "{\"formatVersion\":99,\"leader\":null}");
        File.WriteAllText(sb.GetPath("leader-4.json"), "{\"formatVersion\":1}");
        File.WriteAllText(sb.GetPath("leader-x.json"), "{}");
        File.WriteAllText(sb.GetPath("notes.json"), "{}");

        Assert.That(await store.EnumerateAsync(), Is.EqualTo([
            new LeaderEntry(new LeaderId(1), Uther)
        ]));
        Assert.That(sb.GetTree(), Is.EquivalentTo([
            "leader-1.json",
            "leader-2.json",
            "leader-3.json",
            "leader-4.json",
            "leader-x.json",
            "notes.json"
        ]));
    }
}
