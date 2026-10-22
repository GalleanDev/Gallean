namespace GameContent.Tests;

public sealed class GameUnitSoundsSourceTests
{
    private static UnitTypeSounds Sounds(int beginAttack, int endAttack, int beginAttackHit, int endAttackHit)
    {
        return new UnitTypeSounds
        {
            UnitTypeId = "G000UU0001",
            AttackSounds = ["ATTACK"],
            HitSounds = ["HIT"],
            TouchSounds = ["TOUCH"],
            DamagedSounds = ["DAMAGED"],
            WalkSounds = ["WALK"],
            BeginAttackSoundFrameIndex = beginAttack,
            EndAttackSoundFrameIndex = endAttack,
            BeginAttackHitSoundFrameIndex = beginAttackHit,
            EndAttackHitSoundFrameIndex = endAttackHit
        };
    }

    [Test]
    public void AttackTriggersTest()
    {
        UnitTypeSounds sounds = Sounds(beginAttack: 3, endAttack: 16, beginAttackHit: 7, endAttackHit: 20);

        Assert.That(GameUnitSoundsSource.GetAttackTriggers(sounds), Is.EqualTo(new GameUnitAttackTriggers(Pose: 16, Hit: 7, Second: 20)));
    }

    [Test]
    public void SoundNamesTest()
    {
        UnitTypeSounds sounds = Sounds(beginAttack: 3, endAttack: 9, beginAttackHit: 5, endAttackHit: 0);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(GameUnitSoundsSource.GetSoundNames(sounds, UnitSoundType.Attack), Is.EqualTo(["ATTACK"]));
            Assert.That(GameUnitSoundsSource.GetSoundNames(sounds, UnitSoundType.Hit), Is.EqualTo(["HIT"]));
            Assert.That(GameUnitSoundsSource.GetSoundNames(sounds, UnitSoundType.Touch), Is.EqualTo(["TOUCH"]));
            Assert.That(GameUnitSoundsSource.GetSoundNames(sounds, UnitSoundType.Damaged), Is.EqualTo(["DAMAGED"]));
            Assert.That(GameUnitSoundsSource.GetSoundNames(sounds, UnitSoundType.Walk), Is.EqualTo(["WALK"]));
        }
    }

    [Test]
    public void AnimationFrameIndexTest()
    {
        UnitTypeSounds sounds = Sounds(beginAttack: 3, endAttack: 9, beginAttackHit: 5, endAttackHit: 0);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(GameUnitSoundsSource.GetAnimationFrameIndex(sounds, UnitSoundType.Attack), Is.EqualTo(3));
            Assert.That(GameUnitSoundsSource.GetAnimationFrameIndex(sounds, UnitSoundType.Hit), Is.Null);
            Assert.That(GameUnitSoundsSource.GetAnimationFrameIndex(sounds, UnitSoundType.Touch), Is.Null);
            Assert.That(GameUnitSoundsSource.GetAnimationFrameIndex(sounds, UnitSoundType.Damaged), Is.Null);
            Assert.That(GameUnitSoundsSource.GetAnimationFrameIndex(sounds, UnitSoundType.Walk), Is.Null);
        }
    }
}
