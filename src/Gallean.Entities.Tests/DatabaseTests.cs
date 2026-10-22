using System.Collections;
using System.Reflection;
using System.Text.Json;
using Gallean.Entities.GameData;

namespace Gallean.Entities.Tests;

public sealed class DatabaseTests
{
    private static IReadOnlyList<string> FindEmptyTables(ScenarioDatabase db)
    {
        List<string> result = [];

        foreach (PropertyInfo property in typeof(ScenarioDatabase).GetProperties())
        {
            object? value = property.GetValue(db);

            Assert.That(value, Is.Not.Null);

            if (value is IEnumerable enumerable and not string && IsEmpty(enumerable))
            {
                result.Add(property.Name);
            }
        }

        return result;
    }

    private static bool IsEmpty(IEnumerable enumerable)
    {
        foreach (object item in enumerable)
        {
            return false;
        }

        return true;
    }

    private static IReadOnlyList<string> FindEmptyTableFiles(string directory)
    {
        List<string> result = [];

        foreach (string path in Directory.EnumerateFiles(directory, "*.json", SearchOption.AllDirectories))
        {
            List<JsonElement> rows = DbFileReader.Read(path);

            if (rows.Count <= 0)
            {
                result.Add(Path.GetRelativePath(directory, path));
            }
        }

        return result;
    }

    [Test]
    public void NoEmptyTableFilesTest()
    {
        Assert.That(FindEmptyTableFiles(RiseOfTheElves.RootPath), Is.Empty);
    }

    [Test]
    public void GlobalDatabaseLoadTest()
    {
        GlobalDatabase db = GlobalDatabaseLoader.Load(RiseOfTheElves.GlobalDatabasePath);

        using (Assert.EnterMultipleScope())
        {
            foreach (PropertyInfo prop in typeof(GlobalDatabase).GetProperties())
            {
                object? val = prop.GetValue(db);

                if (val is IEnumerable enumerable and not string)
                {
                    Assert.That(enumerable, Is.Not.Empty);
                }
                else
                {
                    Assert.That(val, Is.Not.Null);
                }
            }
        }
    }

    [Test]
    public void GlobalDatabaseIndexTest()
    {
        GlobalDatabase db = GlobalDatabaseLoader.Load(RiseOfTheElves.GlobalDatabasePath);
        GlobalDatabaseIndex index = GlobalDatabaseLoader.LoadIndex(RiseOfTheElves.GlobalDatabasePath);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(index.Races, Is.EquivalentTo(db.Races));
            Assert.That(index.TextGlobals, Is.EquivalentTo(db.TextGlobals));
        }
    }

    [Test]
    public void EmptyIdTest()
    {
        GlobalDatabase db = GlobalDatabaseLoader.Load(RiseOfTheElves.GlobalDatabasePath);

        Db.Unit baseUnit = db.Units[new Db.Id("g000uu0001")];
        Db.Unit upgradedUnit = db.Units[new Db.Id("g000uu0002")];

        using (Assert.EnterMultipleScope())
        {
            Assert.That(baseUnit.PreviousId, Is.Null);
            Assert.That(upgradedUnit.PreviousId, Is.EqualTo(new Db.Id("g000uu0001")));
        }
    }

    [Test]
    public void ScenarioDatabaseLoadTest()
    {
        ScenarioDatabase db = ScenarioDatabaseLoader.Load(RiseOfTheElves.ScenarioDatabasePath);

        Assert.That(FindEmptyTables(db), Is.EqualTo([
            "ScenarioRods",
            "ScenarioRodsByScenario",
            "ScenarioRodsByScenarioAndRodId"
        ]));
    }

    [Test]
    public void ByScenarioLookupTest()
    {
        ScenarioDatabase db = ScenarioDatabaseLoader.Load(RiseOfTheElves.ScenarioDatabasePath);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(db.ScenarioUnitsByScenario, Is.Not.Empty);

            foreach (IGrouping<Db.ScenarioId, Db.ScenarioUnit> grouping in db.ScenarioUnitsByScenario)
            {
                Db.ScenarioId key = grouping.Key;
                foreach (Db.ScenarioUnit unit in grouping)
                {
                    Assert.That(unit.ScenarioId, Is.EqualTo(key));
                }
            }

            Assert.That(db.ScenarioMapBlocksByScenario, Is.Not.Empty);

            foreach (IGrouping<Db.ScenarioId, Db.ScenarioMapBlock> grouping in db.ScenarioMapBlocksByScenario)
            {
                Db.ScenarioId key = grouping.Key;
                foreach (Db.ScenarioMapBlock block in grouping)
                {
                    Assert.That(block.ScenarioId, Is.EqualTo(key));
                }
            }
        }
    }

    [Test]
    public void UnitsByScenarioAndUnitIdTest()
    {
        ScenarioDatabase db = ScenarioDatabaseLoader.Load(RiseOfTheElves.ScenarioDatabasePath);
        Db.ScenarioId scenarioId = new("S095SC0000");
        Db.Id unitId = new("S095UN0012");

        IReadOnlyDictionary<Db.Id, Db.ScenarioUnit> units = db.ScenarioUnitsByScenarioAndUnitId[scenarioId];

        Assert.That(units[unitId].ScenarioId, Is.EqualTo(scenarioId));
        Assert.That(units[unitId].UnitId, Is.EqualTo(unitId));
    }

    [Test]
    public void UnitModifiersByScenarioAndUnitIdTest()
    {
        ScenarioDatabase db = ScenarioDatabaseLoader.Load(RiseOfTheElves.ScenarioDatabasePath);
        Db.ScenarioId scenarioId = new("S113SC0000");
        Db.Id unitId = new("S113UN00D7");

        ILookup<Db.Id, Db.ScenarioUnitModifier> modifiers = db.ScenarioUnitModifiersByScenarioAndUnitId[scenarioId];
        Db.ScenarioUnitModifier[] unitModifiers = [.. modifiers[unitId]];

        Assert.That(unitModifiers, Has.Length.EqualTo(32));

        foreach (Db.ScenarioUnitModifier modifier in unitModifiers)
        {
            Assert.That(modifier.ScenarioId, Is.EqualTo(scenarioId));
            Assert.That(modifier.UnitId, Is.EqualTo(unitId));
        }
    }

    [Test]
    public void ScenarioDatabaseIndexTest()
    {
        ScenarioDatabase db = ScenarioDatabaseLoader.Load(RiseOfTheElves.ScenarioDatabasePath);
        ScenarioDatabaseIndex index = ScenarioDatabaseLoader.LoadIndex(RiseOfTheElves.ScenarioDatabasePath);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(index.ScenarioInfos, Is.EquivalentTo(db.ScenarioInfos));
            Assert.That(index.ScenariosWithEvents, Is.Not.Empty);
            Assert.That(index.ScenariosWithEvents, Is.EquivalentTo(db.ScenariosWithEvents));
        }
    }

    [Test]
    public void SingleScenarioIndexPresenceTest()
    {
        Db.ScenarioId scenarioId = new("S095SC0000");

        ScenarioDatabase single = ScenarioDatabaseLoader.Load(RiseOfTheElves.ScenarioDatabasePath, scenarioId);

        Assert.That(single.ScenariosWithEvents, Is.EquivalentTo([scenarioId]));
    }

    [Test]
    public void SingleScenarioLoadTest()
    {
        Db.ScenarioId scenarioId = new("S095SC0000");

        ScenarioDatabase db = ScenarioDatabaseLoader.Load(RiseOfTheElves.ScenarioDatabasePath);
        ScenarioDatabase single = ScenarioDatabaseLoader.Load(RiseOfTheElves.ScenarioDatabasePath, scenarioId);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(single.ScenarioUnits, Is.Not.Empty);
            Assert.That(single.ScenarioStacks, Is.Not.Empty);

            Assert.That(single.ScenarioPlayers, Is.EqualTo(db.ScenarioPlayersByScenario[scenarioId]));
            Assert.That(single.ScenarioUnits, Is.EqualTo(db.ScenarioUnitsByScenario[scenarioId]));
            Assert.That(single.ScenarioStacks, Is.EqualTo(db.ScenarioStacksByScenario[scenarioId]));
            Assert.That(single.ScenarioMapBlocks, Is.EqualTo(db.ScenarioMapBlocksByScenario[scenarioId]));
        }
    }
}
