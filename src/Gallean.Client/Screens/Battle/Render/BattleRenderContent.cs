namespace Gallean.Client;

internal sealed record BattleRenderContent(
    BattleGround Ground,
    BattleUnitShaderProgram UnitShaderProgram,
    BattleBackgroundShaderProgram BackgroundShaderProgram,
    IReadOnlyList<BattleFluidShaderProgram> FluidShaderPrograms,
    BattleRainShaderProgram RainShaderProgram,
    BattleShadowShaderProgram ShadowShaderProgram,
    BattleWorldTransitionShaderProgram TransitionShaderProgram,
    double ShadowStrength);
