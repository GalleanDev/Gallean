using System.Collections.Immutable;

namespace Gallean.Engine;

public sealed record GlobalVariables
{
    internal int AccuracyBonusAiEasy { get; init; } = -15;
    internal int AccuracyBonusAiAverage { get; init; } = 0;
    internal int AccuracyBonusAiHard { get; init; } = 5;
    internal int AccuracyBonusAiVeryHard { get; init; } = 10;
    internal int AccuracyReductionPerTarget { get; init; } = 10;
    internal int ActionMovementCostPercent { get; init; } = 50;
    internal int AiDefaultPriority { get; init; } = 3;
    internal int AiGlobalSpellMinMana { get; init; } = 1000;
    internal int AiGlobalSpellMinStacks { get; init; } = 2;
    internal int AiGlobalSpellMinSuggestedLevel { get; init; } = 10;
    internal int AiObjectiveTurnEasy { get; init; } = 60;
    internal int AiObjectiveTurnAverage { get; init; } = 50;
    internal int AiObjectiveTurnHard { get; init; } = 40;
    internal int AiObjectiveTurnVeryHard { get; init; } = 30;
    internal int AiStackDefendRange { get; init; } = 5;
    internal int AiStackGuardRange { get; init; } = 5;
    internal int BagRange { get; init; } = 1;
    internal int BattleDamageVariance { get; init; } = 6;
    internal int BattleInitiativeVariance { get; init; } = 10;
    internal int BribeCostMultiplier { get; init; } = 3;
    internal int CapitalCaptureSpellChance { get; init; } = 50;
    internal int CapitalCityLevel { get; init; } = 6;
    internal int CapitalTerrainTilesPerTurn { get; init; } = 10;
    internal int CapitalVisionRadius { get; init; } = 6;
    internal int CityCaptureTerrainPercent { get; init; } = 100;
    internal int CityTerrainTilesPerTurnLevel1 { get; init; } = 15;
    internal int CityTerrainTilesPerTurnLevel2 { get; init; } = 15;
    internal int CityTerrainTilesPerTurnLevel3 { get; init; } = 20;
    internal int CityTerrainTilesPerTurnLevel4 { get; init; } = 20;
    internal int CityTerrainTilesPerTurnLevel5 { get; init; } = 25;
    internal int CriticalHitAccuracy { get; init; } = 100;
    internal int CriticalHitDamagePercent { get; init; } = 5;
    internal int DamageOverTimeMaxDurationTurns { get; init; } = 4;
    internal int DamageOverTimeMinDurationTurns { get; init; } = 2;
    internal int DefendArmorBonus { get; init; } = 50;
    internal int DiplomacyAllianceChancePenalty { get; init; } = 10;
    internal int DiplomacyAllianceMinChance { get; init; } = 5;
    internal int DiplomacyAttackAtPeaceRelation { get; init; } = 66;
    internal int DiplomacyAttackFortificationRelationChange { get; init; } = -10;
    internal int DiplomacyAttackStackRelationChange { get; init; } = -7;
    internal int DiplomacyBreakAllianceRelation { get; init; } = 10;
    internal int DiplomacyDestroyRodRelationChange { get; init; } = -15;
    internal int DiplomacyExchangeMapChance { get; init; } = 30;
    internal int DiplomacyFairTradeChance { get; init; } = 50;
    internal int DiplomacyGiftGoldPerRelationChange { get; init; } = 50;
    internal int DiplomacyMakeAllianceRelation { get; init; } = 100;
    internal int DiplomacyNeutralRelation { get; init; } = 66;
    internal int DiplomacyPeaceRelation { get; init; } = 100;
    internal int DiplomacyRefuseAllianceRelation { get; init; } = 50;
    internal int DiplomacyThiefActionRelationChange { get; init; } = -5;
    internal int DiplomacyWarRelation { get; init; } = 33;
    internal int DisableEffectMinDurationTurns { get; init; } = 1;
    internal int DisableEffectMaxDurationTurns { get; init; } = 3;
    internal int DoppelgangerEffectDurationTurns { get; init; } = 1;
    internal ImmutableArray<UnitDefinitionId> DoppelgangerBlockList { get; init; } = [new("G000UU8003"), new("G000UU8006"), new("G000UU8103"), new("G000UU8106"), new("G000UU8150")];
    internal int DrainHealPercent { get; init; } = 50;
    internal int DrainOverflowHealPercent { get; init; } = 50;
    internal int ExperienceBonusAiEasy { get; init; } = 0;
    internal int ExperienceBonusAiAverage { get; init; } = 25;
    internal int ExperienceBonusAiHard { get; init; } = 50;
    internal int ExperienceBonusAiMinSuggestedLevel { get; init; } = 10;
    internal int ExperienceBonusAiVeryHard { get; init; } = 100;
    internal int ExperienceBonusHumanEasy { get; init; } = 20;
    internal int ExperienceBonusHumanAverage { get; init; } = 0;
    internal int ExperienceBonusHumanHard { get; init; } = 0;
    internal int ExperienceBonusHumanVeryHard { get; init; } = 0;
    internal int ExperienceBonusWeaponMaster { get; init; } = 25;
    internal int IncomeBonusPercentEasy { get; init; } = 30;
    internal int IncomeBonusPercentAverage { get; init; } = 0;
    internal int IncomeBonusPercentHard { get; init; } = 0;
    internal int IncomeBonusPercentVeryHard { get; init; } = 0;
    internal int IncreaseDamagePercentLevel1 { get; init; } = 25;
    internal int IncreaseDamagePercentLevel2 { get; init; } = 50;
    internal int IncreaseDamagePercentLevel3 { get; init; } = 75;
    internal int IncreaseDamagePercentLevel4 { get; init; } = 100;
    internal int LeaderExportItemLimit { get; init; } = 5;
    internal int LordGuildmasterCityGrowthDiscountPercent { get; init; } = 50;
    internal int LordGuildmasterSpellCastsPerTurn { get; init; } = 1;
    internal int LordGuildmasterSpellResearchLevelLimit { get; init; } = 4;
    internal int LordMageSpellCastsPerTurn { get; init; } = 2;
    internal int LordMageSpellResearchDiscountPercent { get; init; } = 50;
    internal int LordMageSpellResearchLevelLimit { get; init; } = 5;
    internal int LordWarriorRegeneration { get; init; } = 15;
    internal int LordWarriorSpellCastsPerTurn { get; init; } = 1;
    internal int LordWarriorSpellResearchLevelLimit { get; init; } = 4;
    internal int MaxArmor { get; init; } = 90;
    internal int MaxAttackAccuracy { get; init; } = 100;
    internal int MaxAttackDamage { get; init; } = 300;
    internal int MaxAttackInitiative { get; init; } = 150;
    internal int MaxLeaderAbilities { get; init; } = 15;
    internal int MaxModifiedAttackDamage { get; init; } = 400;
    internal int MaxStackMovement { get; init; } = 100;
    internal int MaxStackVisionRadius { get; init; } = 8;
    internal int MaxHitpoints { get; init; } = 9999;
    internal int MaxPurchaseDiscountPercent { get; init; } = 100;
    internal int MinArmor { get; init; } = 0;
    internal int MinAttackAccuracy { get; init; } = 0;
    internal int MinAttackDamage { get; init; } = 1;
    internal int MinAttackInitiative { get; init; } = 1;
    internal int MinHitpoints { get; init; } = 1;
    internal int MinStackMovement { get; init; } = 0;
    internal int MinStackVisionRadius { get; init; } = 1;
    internal int NativeTerrainRegeneration { get; init; } = 10;
    internal int PoisonCityDamagePercent { get; init; } = 25;
    internal int PoisonStackDamagePercent { get; init; } = 20;
    internal int PotionBoostDurationTurns { get; init; } = 1;
    internal int ReduceDamagePercentLevel1 { get; init; } = 50;
    internal int ReduceDamagePercentLevel2 { get; init; } = 33;
    internal int ReduceInitiativePercent { get; init; } = 50;
    internal int ReduceLevelEffectDurationTurns { get; init; } = 1;
    internal int ResourceNodeIncome { get; init; } = 50;
    internal int ResurrectionHitpoints { get; init; } = 1;
    internal int RiotDamagePercent { get; init; } = 20;
    internal int RiotMaxTurns { get; init; } = 4;
    internal int RiotMinTurns { get; init; } = 1;
    internal WorldResourceSet RodCost { get; init; } = new(150, 0, 0, 0, 0, 0);
    internal int RuinGarrisonSize { get; init; } = 6;
    internal int RuinRegeneration { get; init; } = 20;
    internal int RodPlantRange { get; init; } = 1;
    internal int RodRemoveRange { get; init; } = 2;
    internal int RodSpacingRange { get; init; } = 2;
    internal int RodTerrainTilesPerTurn { get; init; } = 2;
    internal ImmutableArray<ScenarioStackReference> ScenarioKeyStacks { get; init; } =
    [
        new ScenarioStackReference.Stack(new WorldStackId("S095KC0027")),
        new ScenarioStackReference.Stack(new WorldStackId("S096KC0042")),
        new ScenarioStackReference.Stack(new WorldStackId("S097KC0005")),
        new ScenarioStackReference.Stack(new WorldStackId("S098KC0012")),
        new ScenarioStackReference.Template(new ScenarioStackTemplateId("S098TM0002")),
        new ScenarioStackReference.Stack(new WorldStackId("S099KC005A")),
        new ScenarioStackReference.Stack(new WorldStackId("S100KC0002")),
        new ScenarioStackReference.Stack(new WorldStackId("S103KC002B")),
        new ScenarioStackReference.Stack(new WorldStackId("S104KC0068")),
        new ScenarioStackReference.Stack(new WorldStackId("S105KC0002")),
        new ScenarioStackReference.Stack(new WorldStackId("S106KC006C")),
        new ScenarioStackReference.Stack(new WorldStackId("S106KC006F")),
        new ScenarioStackReference.Stack(new WorldStackId("S107KC0020")),
        new ScenarioStackReference.Stack(new WorldStackId("S109KC003F")),
        new ScenarioStackReference.Stack(new WorldStackId("S109KC0046")),
        new ScenarioStackReference.Stack(new WorldStackId("S110KC0013")),
        new ScenarioStackReference.Stack(new WorldStackId("S111KC0004")),
        new ScenarioStackReference.Stack(new WorldStackId("S112KC003C")),
        new ScenarioStackReference.Stack(new WorldStackId("S113KC005D")),
        new ScenarioStackReference.Stack(new WorldStackId("S114KC0004")),
        new ScenarioStackReference.Stack(new WorldStackId("S114KC0006")),
        new ScenarioStackReference.Stack(new WorldStackId("S114KC0008")),
        new ScenarioStackReference.Stack(new WorldStackId("S114KC0009")),
        new ScenarioStackReference.Stack(new WorldStackId("S115KC0045")),
        new ScenarioStackReference.Template(new ScenarioStackTemplateId("S117TM0000")),
        new ScenarioStackReference.Template(new ScenarioStackTemplateId("S118TM0003")),
        new ScenarioStackReference.Stack(new WorldStackId("S121KC0007")),
        new ScenarioStackReference.Stack(new WorldStackId("S122KC0004")),
        new ScenarioStackReference.Template(new ScenarioStackTemplateId("S122TM0007")),
        new ScenarioStackReference.Template(new ScenarioStackTemplateId("S122TM0008")),
        new ScenarioStackReference.Stack(new WorldStackId("S136KC000B")),
        new ScenarioStackReference.Stack(new WorldStackId("S137KC0003")),
        new ScenarioStackReference.Stack(new WorldStackId("S137KC0004")),
        new ScenarioStackReference.Stack(new WorldStackId("S137KC001B")),
        new ScenarioStackReference.Template(new ScenarioStackTemplateId("S137TM0000")),
        new ScenarioStackReference.Stack(new WorldStackId("S138KC0035")),
        new ScenarioStackReference.Template(new ScenarioStackTemplateId("S139TM000A")),
        new ScenarioStackReference.Stack(new WorldStackId("S140KC0024")),
        new ScenarioStackReference.Stack(new WorldStackId("S141KC0006")),
        new ScenarioStackReference.Stack(new WorldStackId("S141KC0008")),
        new ScenarioStackReference.Stack(new WorldStackId("S141KC0055")),
        new ScenarioStackReference.Stack(new WorldStackId("S142KC000D")),
        new ScenarioStackReference.Stack(new WorldStackId("S142KC0013")),
        new ScenarioStackReference.Template(new ScenarioStackTemplateId("S142TM0000"))
    ];
    internal int SellItemPercent { get; init; } = 20;
    internal int ShatterArmorPerHitCap { get; init; } = 100;
    internal int ShatterArmorTotalCap { get; init; } = 100;
    internal int SpellModifierDurationTurns { get; init; } = 1;
    internal int SpellPowerRing0 { get; init; } = 100;
    internal int SpellPowerRing1 { get; init; } = 80;
    internal int SpellPowerRing2 { get; init; } = 60;
    internal int SpellPowerRing3 { get; init; } = 40;
    internal int SpellPowerRing4 { get; init; } = 30;
    internal int SpyDiscoverChance { get; init; } = 10;
    internal int StartingResourcesPercentEasy { get; init; } = 300;
    internal int StartingResourcesPercentAverage { get; init; } = 100;
    internal int StartingResourcesPercentHard { get; init; } = 50;
    internal int StartingResourcesPercentVeryHard { get; init; } = 25;
    internal int StealGoldPlayerPercent { get; init; } = 25;
    internal int TalismanCharges { get; init; } = 5;
    internal int ThiefProtectionCityLevel1 { get; init; } = 10;
    internal int ThiefProtectionCityLevel2 { get; init; } = 20;
    internal int ThiefProtectionCityLevel3 { get; init; } = 30;
    internal int ThiefProtectionCityLevel4 { get; init; } = 40;
    internal int ThiefProtectionCityLevel5 { get; init; } = 50;
    internal int ThiefProtectionCityLevel6 { get; init; } = 60;
    internal int TransformEffectMaxDurationTurns { get; init; } = 4;
    internal int TransformEffectMinDurationTurns { get; init; } = 2;
}
