namespace Gallean.Client;

internal sealed record TrainerInspectedUnitViewModel(
    AssetSpriteKey.UnitEventPortrait PortraitKey,
    string Name,
    string GainText,
    int Experience,
    int NextLevelExperience,
    int TrainableExperience,
    string ExperienceText,
    bool IsMaximumLevel,
    bool IsPendingUpgrade)
{
    public bool HasTrainableExperience => TrainableExperience > 0;
}
