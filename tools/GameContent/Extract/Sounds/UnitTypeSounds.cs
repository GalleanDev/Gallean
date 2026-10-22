namespace GameContent;

public sealed record UnitTypeSounds
{
    public required string UnitTypeId { get; init; }

    public IReadOnlyList<string> AttackSounds { get; init; } = [];

    public int BeginAttackSoundFrameIndex { get; init; }
    public int EndAttackSoundFrameIndex { get; init; }

    public IReadOnlyList<string> HitSounds { get; init; } = [];

    public int BeginAttackHitSoundFrameIndex { get; init; }
    public int EndAttackHitSoundFrameIndex { get; init; }

    public IReadOnlyList<string> TouchSounds { get; init; } = [];
    public IReadOnlyList<string> DamagedSounds { get; init; } = [];
    public IReadOnlyList<string> WalkSounds { get; init; } = [];
}
