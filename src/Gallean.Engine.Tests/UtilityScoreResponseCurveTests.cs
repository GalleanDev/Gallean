namespace Gallean.Engine.Tests;

public sealed class UtilityScoreResponseCurveTests
{
    private static int[] Sample(UtilityScore.ResponseCurve curve, int[] inputs)
    {
        int[] outputs = new int[inputs.Length];

        for (int index = 0; index < inputs.Length; index++)
        {
            outputs[index] = curve.Evaluate(inputs[index]);
        }

        return outputs;
    }

    [Test]
    public void LinearTest()
    {
        Assert.That(Sample(UtilityScore.ResponseCurve.Linear, [0, 250, 500, 1000]), Is.EqualTo([0, 250, 500, 1000]));
    }

    [Test]
    public void LinearRangeTest()
    {
        Assert.That(Sample(UtilityScore.ResponseCurve.Linear, [-100, 1500]), Is.EqualTo([0, 1000]));
    }

    [Test]
    public void PolynomialTest()
    {
        UtilityScore.ResponseCurve curve = new UtilityScore.ResponseCurve.Polynomial(Slope: 1000, Exponent: 2, XShift: 0, YShift: 0);

        Assert.That(Sample(curve, [0, 500, 1000]), Is.EqualTo([0, 250, 1000]));
    }

    [Test]
    public void PolynomialYShiftTest()
    {
        UtilityScore.ResponseCurve curve = new UtilityScore.ResponseCurve.Polynomial(Slope: 900, Exponent: 1, XShift: 0, YShift: 100);

        Assert.That(Sample(curve, [0, 1000]), Is.EqualTo([100, 1000]));
    }

    [Test]
    public void RootTest()
    {
        UtilityScore.ResponseCurve curve = new UtilityScore.ResponseCurve.Root(Slope: 1000, Exponent: 2, XShift: 0, YShift: 0);

        Assert.That(Sample(curve, [0, 250, 1000]), Is.EqualTo([0, 500, 1000]));
    }

    [Test]
    public void RootInvertsPolynomialTest()
    {
        UtilityScore.ResponseCurve polynomial = new UtilityScore.ResponseCurve.Polynomial(Slope: 1000, Exponent: 3, XShift: 0, YShift: 0);
        UtilityScore.ResponseCurve root = new UtilityScore.ResponseCurve.Root(Slope: 1000, Exponent: 3, XShift: 0, YShift: 0);

        Assert.That(Sample(root, Sample(polynomial, [0, 250, 500, 750, 1000])), Is.EqualTo([0, 246, 500, 749, 1000]));
    }

    [Test]
    public void ThresholdTest()
    {
        UtilityScore.ResponseCurve curve = new UtilityScore.ResponseCurve.Threshold(Point: 500, LowScore: 250, HighScore: 1000);

        Assert.That(Sample(curve, [0, 499, 500, 1000]), Is.EqualTo([250, 250, 1000, 1000]));
    }

    [Test]
    public void LogisticTest()
    {
        UtilityScore.ResponseCurve curve = new UtilityScore.ResponseCurve.Logistic(Slope: 4000, Midpoint: 500, YShift: 0);

        Assert.That(Sample(curve, [0, 250, 500, 750, 1000]), Is.EqualTo([167, 250, 500, 750, 833]));
    }

    [Test]
    public void LogisticYShiftTest()
    {
        UtilityScore.ResponseCurve curve = new UtilityScore.ResponseCurve.Logistic(Slope: 4000, Midpoint: 500, YShift: 200);

        Assert.That(Sample(curve, [0, 500, 1000]), Is.EqualTo([333, 600, 866]));
    }

    [Test]
    public void LogitSaturatesTest()
    {
        UtilityScore.ResponseCurve curve = new UtilityScore.ResponseCurve.Logit(Slope: 1000, Midpoint: 500);

        Assert.That(Sample(curve, [0, 375, 500, 625, 1000]), Is.EqualTo([0, 334, 500, 666, 1000]));
    }

    [Test]
    public void BellTest()
    {
        UtilityScore.ResponseCurve curve = new UtilityScore.ResponseCurve.Bell(Center: 500, Width: 500, Height: 1000, YShift: 0);

        Assert.That(Sample(curve, [0, 250, 500, 750, 1000]), Is.EqualTo([0, 750, 1000, 750, 0]));
    }
}
