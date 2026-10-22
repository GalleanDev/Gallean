namespace Packer.Tests;

public sealed class UmbraSpriteHashTests
{
    [Test]
    public void SamePixelsTest()
    {
        AssetSprite sprite = new(new AssetSpriteMetadata(2, 3, 2, 3, 0, 0, Additive: false), new byte[] { 1, 2, 3, 4 });
        AssetSprite same = new(new AssetSpriteMetadata(2, 3, 2, 3, 4, 5, Additive: true), new byte[] { 1, 2, 3, 4 });

        Assert.That(same.ToHash(), Is.EqualTo(sprite.ToHash()));
    }

    [Test]
    public void TransposedShapesTest()
    {
        AssetSprite portrait = new(new AssetSpriteMetadata(2, 3, 2, 3, 0, 0, Additive: false), new byte[2 * 3 * 4]);
        AssetSprite landscape = new(new AssetSpriteMetadata(3, 2, 3, 2, 0, 0, Additive: false), new byte[3 * 2 * 4]);

        Assert.That(landscape.ToHash(), Is.Not.EqualTo(portrait.ToHash()));
    }
}
