namespace Gallean.Engine.Tests;

public sealed class UtilityScoreTests
{
    private static UtilityScore.Consideration Axis(int input)
    {
        return new UtilityScore.Consideration(input, UtilityScore.ResponseCurve.Linear);
    }

    [Test]
    public void OfIdentityTest()
    {
        Assert.That(UtilityScore.Of(400).Score, Is.EqualTo(400));
    }

    [Test]
    public void OfOutOfRangeTest()
    {
        Assert.That(UtilityScore.Of(-100).Score, Is.Zero);
        Assert.That(UtilityScore.Of(1500).Score, Is.EqualTo(1000));
    }

    [Test]
    public void FullTest()
    {
        Assert.That(UtilityScore.Full.Score, Is.EqualTo(1000));
    }

    [Test]
    public void NoneTest()
    {
        Assert.That(UtilityScore.None.Score, Is.Zero);
    }

    [Test]
    public void FlagTest()
    {
        Assert.That(UtilityScore.Flag(true).Score, Is.EqualTo(1000));
        Assert.That(UtilityScore.Flag(false).Score, Is.Zero);
    }

    [Test]
    public void FlagWithPenaltyTest()
    {
        Assert.That(UtilityScore.Flag(true, 250).Score, Is.EqualTo(1000));
        Assert.That(UtilityScore.Flag(false, 250).Score, Is.EqualTo(250));
    }

    [Test]
    public void RatioTest()
    {
        Assert.That(UtilityScore.Ratio(1, 4).Score, Is.EqualTo(250));
        Assert.That(UtilityScore.Ratio(3, 4).Score, Is.EqualTo(750));
        Assert.That(UtilityScore.Ratio(4, 4).Score, Is.EqualTo(1000));
    }

    [Test]
    public void RatioZeroWholeTest()
    {
        Assert.That(UtilityScore.Ratio(0, 0).Score, Is.EqualTo(1000));
    }

    [Test]
    public void InverseRatioTest()
    {
        Assert.That(UtilityScore.InverseRatio(1, 4).Score, Is.EqualTo(750));
        Assert.That(UtilityScore.InverseRatio(3, 4).Score, Is.EqualTo(250));
    }

    [Test]
    public void InverseRatioZeroWholeTest()
    {
        Assert.That(UtilityScore.InverseRatio(5, 0).Score, Is.EqualTo(1000));
    }

    [Test]
    public void InverseRatioTruncateTest()
    {
        Assert.That(UtilityScore.InverseRatio(1, 3).Score, Is.EqualTo(667));
    }

    [Test]
    public void RatioWithCurveTest()
    {
        UtilityScore.ResponseCurve curve = new UtilityScore.ResponseCurve.Polynomial(Slope: 300, Exponent: 1, XShift: 0, YShift: 700);

        Assert.That(UtilityScore.Ratio(0, 4, curve).Score, Is.EqualTo(700));
        Assert.That(UtilityScore.Ratio(3, 4, curve).Score, Is.EqualTo(925));
        Assert.That(UtilityScore.Ratio(4, 4, curve).Score, Is.EqualTo(1000));
    }

    [Test]
    public void RatioWithCurveAboveWholeTest()
    {
        UtilityScore.ResponseCurve curve = new UtilityScore.ResponseCurve.Polynomial(Slope: 300, Exponent: 1, XShift: 0, YShift: 700);

        Assert.That(UtilityScore.Ratio(8, 4, curve).Score, Is.EqualTo(1000));
    }

    [Test]
    public void RatioWithCurveZeroWholeTest()
    {
        UtilityScore.ResponseCurve curve = new UtilityScore.ResponseCurve.Polynomial(Slope: 300, Exponent: 1, XShift: 0, YShift: 700);

        Assert.That(UtilityScore.Ratio(5, 0, curve).Score, Is.EqualTo(1000));
    }

    [Test]
    public void CombineEmptyTest()
    {
        Assert.That(UtilityScore.Combine([]), Is.EqualTo(1000));
    }

    [Test]
    public void CombineSingleTest()
    {
        Assert.That(UtilityScore.Combine([Axis(500)]), Is.EqualTo(500));
    }

    [Test]
    public void CombineTwoAxesTest()
    {
        Assert.That(UtilityScore.Combine([Axis(500), Axis(500)]), Is.EqualTo(250));
    }

    [Test]
    public void CombineThreeAxesTest()
    {
        Assert.That(UtilityScore.Combine([Axis(500), Axis(500), Axis(500)]), Is.EqualTo(125));
    }

    [Test]
    public void CombineWeakAxisTest()
    {
        Assert.That(UtilityScore.Combine([Axis(1000), Axis(100)]), Is.EqualTo(100));
    }

    [Test]
    public void CombineWeakChainTest()
    {
        Assert.That(UtilityScore.Combine([Axis(100), Axis(100), Axis(100), Axis(100)]), Is.EqualTo(1));
    }

    [Test]
    public void CombineFlagVetoTest()
    {
        Assert.That(UtilityScore.Combine([UtilityScore.Of(1000), UtilityScore.Flag(false)]), Is.Zero);
    }
}
