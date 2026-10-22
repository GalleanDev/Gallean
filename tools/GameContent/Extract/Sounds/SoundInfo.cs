namespace GameContent;

internal sealed record SoundInfo
{
    public int UnitTypeId { get; init; }

    public required string AttackListName { get; init; }
    public int BeginAttackSoundFrameIndex { get; init; }
    public int EndAttackSoundFrameIndex { get; init; }

    public required string HitListName { get; init; }
    public int BeginAttackHitSoundFrameIndex { get; init; }
    public int EndAttackHitSoundFrameIndex { get; init; }

    public required string TouchListName { get; init; }
    public required string DamagedListName { get; init; }
    public required string WalkListName { get; init; }
}
