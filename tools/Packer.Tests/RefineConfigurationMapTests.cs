using Gallean.Engine.Contracts;

namespace Packer.Tests;

public sealed class RefineConfigurationMapTests
{
    [Test]
    public void BattleUnitTest()
    {
        AssetAnimationKey.BattleUnit key = new(new UnitDefinitionId("g000uu0001"), AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Attacker);

        Assert.That(RefineConfigurationMap.GetConfiguration(key), Is.EqualTo(new RefineConfiguration(MinClipDimension: 90)));
    }

    [Test]
    public void CursorTest()
    {
        AssetAnimationKey.Cursor key = new(AssetAnimationKey.CursorType.Default);

        Assert.That(RefineConfigurationMap.GetConfiguration(key), Is.EqualTo(new RefineConfiguration(MinClipDimension: 128)));
    }
}
