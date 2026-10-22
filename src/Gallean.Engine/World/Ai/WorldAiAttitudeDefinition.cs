namespace Gallean.Engine;

public sealed record WorldAiAttitudeDefinition(
    WorldAiAttitudeType Type,
    int NeutralStackInterest,
    int AiPlayerStackInterest,
    int HumanPlayerStackInterest,
    int NeutralCityInterest,
    int AiPlayerCityInterest,
    int HumanPlayerCityInterest,
    int BagInterest,
    int RuinInterest,
    int EasyIncomePercent,
    int AverageIncomePercent,
    int HardIncomePercent,
    int VeryHardIncomePercent);
