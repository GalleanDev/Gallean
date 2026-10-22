namespace Gallean.Engine;

public abstract record ScenarioStackReference
{
    public sealed record Stack(WorldStackId StackId) : ScenarioStackReference;

    public sealed record Template(ScenarioStackTemplateId TemplateId) : ScenarioStackReference;
}
