using System.Text.Json;

namespace Extractor;

internal static class ScenarioTableWriter
{
    public static void WriteScenario(string scenarioDirectory, ScenarioFileTables tables)
    {
        Directory.CreateDirectory(scenarioDirectory);

        Write(scenarioDirectory, "SBag.json", tables.ScenarioBags, ScenarioRowWriter.Write);
        Write(scenarioDirectory, "SCrys.json", tables.ScenarioCrystals, ScenarioRowWriter.Write);
        Write(scenarioDirectory, "SEvCond.json", tables.ScenarioEventConditions, ScenarioRowWriter.Write);
        Write(scenarioDirectory, "SEvEffec.json", tables.ScenarioEventEffects, ScenarioRowWriter.Write);
        Write(scenarioDirectory, "SEvent.json", tables.ScenarioEvents, ScenarioRowWriter.Write);
        Write(scenarioDirectory, "SEvUnfog.json", tables.ScenarioEventUnfogs, ScenarioRowWriter.Write);
        Write(scenarioDirectory, "Sfog.json", tables.ScenarioFogs, ScenarioRowWriter.Write);
        Write(scenarioDirectory, "SFort.json", tables.ScenarioFortifications, ScenarioRowWriter.Write);
        Write(scenarioDirectory, "SInventr.json", tables.ScenarioInventories, ScenarioRowWriter.Write);
        Write(scenarioDirectory, "SItem.json", tables.ScenarioItems, ScenarioRowWriter.Write);
        Write(scenarioDirectory, "SLmark.json", tables.ScenarioLandmarks, ScenarioRowWriter.Write);
        Write(scenarioDirectory, "SLoc.json", tables.ScenarioLocations, ScenarioRowWriter.Write);
        Write(scenarioDirectory, "SMage.json", tables.ScenarioMages, ScenarioRowWriter.Write);
        Write(scenarioDirectory, "SMapBlck.json", tables.ScenarioMapBlocks, ScenarioRowWriter.Write);
        Write(scenarioDirectory, "SMapEl.json", tables.ScenarioMapElements, ScenarioRowWriter.Write);
        Write(scenarioDirectory, "SMercs.json", tables.ScenarioMercenaries, ScenarioRowWriter.Write);
        Write(scenarioDirectory, "SMerch.json", tables.ScenarioMerchants, ScenarioRowWriter.Write);
        Write(scenarioDirectory, "SMounts.json", tables.ScenarioMountains, ScenarioRowWriter.Write);
        Write(scenarioDirectory, "SObjMap.json", tables.ScenarioObjectMaps, ScenarioRowWriter.Write);
        Write(scenarioDirectory, "Splayer.json", tables.ScenarioPlayers, ScenarioRowWriter.Write);
        Write(scenarioDirectory, "SRel.json", tables.ScenarioRelations, ScenarioRowWriter.Write);
        Write(scenarioDirectory, "SRoad.json", tables.ScenarioRoads, ScenarioRowWriter.Write);
        Write(scenarioDirectory, "SRod.json", tables.ScenarioRods, ScenarioRowWriter.Write);
        Write(scenarioDirectory, "SRuin.json", tables.ScenarioRuins, ScenarioRowWriter.Write);
        Write(scenarioDirectory, "SSite.json", tables.ScenarioSites, ScenarioRowWriter.Write);
        Write(scenarioDirectory, "Sspell.json", tables.ScenarioSpells, ScenarioRowWriter.Write);
        Write(scenarioDirectory, "Sstack.json", tables.ScenarioStacks, ScenarioRowWriter.Write);
        Write(scenarioDirectory, "SStkTemp.json", tables.ScenarioStackTemplates, ScenarioRowWriter.Write);
        Write(scenarioDirectory, "SStkTemM.json", tables.ScenarioStackTemplateModifiers, ScenarioRowWriter.Write);
        Write(scenarioDirectory, "SSubRace.json", tables.ScenarioSubraces, ScenarioRowWriter.Write);
        Write(scenarioDirectory, "STalisCh.json", tables.ScenarioTalismanCharges, ScenarioRowWriter.Write);
        Write(scenarioDirectory, "SUnit.json", tables.ScenarioUnits, ScenarioRowWriter.Write);
        Write(scenarioDirectory, "SUGroup.json", tables.ScenarioUnitGroups, ScenarioRowWriter.Write);
        Write(scenarioDirectory, "SUnitM.json", tables.ScenarioUnitModifiers, ScenarioRowWriter.Write);
        Write(scenarioDirectory, "SVars.json", tables.ScenarioVariables, ScenarioRowWriter.Write);
        Write(scenarioDirectory, "Tscen.json", tables.TextScenarios, ScenarioRowWriter.Write);
    }

    public static void AppendScenarioInfos(string directory, IReadOnlyList<Db.ScenarioInfo> infos)
    {
        Append(directory, "Sinfo.json", infos, ScenarioRowWriter.Write);
    }

    public static void AppendCampaignScenarios(string directory, IReadOnlyList<Db.CampaignScenario> scenarios)
    {
        Append(directory, "CScen.json", scenarios, ScenarioRowWriter.Write);
    }

    private static void Append<T>(string directory, string fileName, IReadOnlyList<T> rows, Action<Utf8JsonWriter, T> writeRow)
    {
        if (rows.Count > 0)
        {
            string path = Path.Combine(directory, fileName);
            IReadOnlyList<JsonElement> existing = [];

            if (File.Exists(path))
            {
                existing = DbFileReader.Read(path);
            }

            Write(path, existing, rows, writeRow);
        }
    }

    private static void Write<T>(string directory, string fileName, IReadOnlyList<T> rows, Action<Utf8JsonWriter, T> writeRow)
    {
        if (rows.Count > 0)
        {
            Write(Path.Combine(directory, fileName), [], rows, writeRow);
        }
    }

    private static void Write<T>(string path, IReadOnlyList<JsonElement> existing, IReadOnlyList<T> rows, Action<Utf8JsonWriter, T> writeRow)
    {
        DbFileWriter.Write(path, writer =>
        {
            foreach (JsonElement row in existing)
            {
                row.WriteTo(writer);
            }

            foreach (T row in rows)
            {
                writeRow(writer, row);
            }
        });
    }
}
