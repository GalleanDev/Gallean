namespace Gallean.Engine;

internal enum WorldPlayerBuildingStatus
{
    Acquired,
    Constructible,
    MissingPrerequisite,
    BranchLocked,
    ScenarioLocked
}
