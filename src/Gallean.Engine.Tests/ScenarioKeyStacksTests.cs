using Gallean.Entities.GameData;

namespace Gallean.Engine.Tests;

public sealed class ScenarioKeyStacksTests
{
    private static string GetId(ScenarioStackReference reference)
    {
        return reference switch
        {
            ScenarioStackReference.Stack stack => stack.StackId.Value,
            ScenarioStackReference.Template template => template.TemplateId.Value,
            _ => throw new ArgumentOutOfRangeException(nameof(reference), reference, null)
        };
    }

    private static bool HasTemplate(Scenario scenario, ScenarioStackTemplateId templateId)
    {
        foreach (ScenarioStackTemplateDefinition definition in scenario.Provider.StackTemplates)
        {
            if (definition.Id == templateId)
            {
                return true;
            }
        }

        return false;
    }

    private static bool Exists(ScenarioStackReference reference, Scenario scenario)
    {
        return reference switch
        {
            ScenarioStackReference.Stack stack => scenario.State.Stacks.ContainsKey(stack.StackId),
            ScenarioStackReference.Template template => HasTemplate(scenario, template.TemplateId),
            _ => throw new ArgumentOutOfRangeException(nameof(reference), reference, null)
        };
    }

    [Test]
    public void ReferencesExistTest()
    {
        GlobalDatabase globalDb = GlobalDatabaseLoader.Load(RiseOfTheElves.GlobalDatabasePath);
        ScenarioDatabase scenarioDb = ScenarioDatabaseLoader.Load(RiseOfTheElves.ScenarioDatabasePath);
        CampaignDatabase campaignDb = CampaignDatabaseLoader.Load(RiseOfTheElves.CampaignDatabasePath);
        GameDefinitionProvider definitions = new(globalDb);
        GlobalVariables globals = new();
        List<string> missing = [];

        foreach (ScenarioStackReference reference in globals.ScenarioKeyStacks)
        {
            string id = GetId(reference);
            Scenario scenario = ScenarioLoader.Load(globalDb, scenarioDb, campaignDb, new ScenarioId($"{id[..4]}SC0000"), definitions, globals);

            if (!Exists(reference, scenario))
            {
                missing.Add(id);
            }
        }

        Assert.That(missing, Is.Empty);
    }
}
