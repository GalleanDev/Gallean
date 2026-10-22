using TorchSharp;

namespace Umbra.Tests;

public sealed class TeaCacheTests
{
    private static List<string> Run(TeaCacheOptions options, int forwards, double drift)
    {
        using TeaCache cache = new(options);

        List<string> outcomes = [];

        for (int index = 0; index < forwards; index++)
        {
            using torch.Tensor modulation = torch.full([1, 6, 4], 1.0 + (drift * index), torch.ScalarType.Float32);
            using torch.Tensor working = torch.zeros([1, 8, 4], torch.ScalarType.Float32);

            bool transformed = false;

            using torch.Tensor result = cache.Apply(
                working.alias(),
                modulation,
                input =>
                {
                    transformed = true;

                    return input.add(1.0);
                });

            if (transformed)
            {
                outcomes.Add("computed");
            }
            else
            {
                outcomes.Add("skipped");
            }
        }

        return outcomes;
    }

    [Test]
    public void NoneTest()
    {
        List<string> outcomes = Run(TeaCacheOptions.None, 6, 0.0);

        Assert.That(outcomes, Is.EqualTo(["computed", "computed", "computed", "computed", "computed", "computed"]));
    }

    [Test]
    public void SkipStableStepsTest()
    {
        TeaCacheOptions options = new(1.0, [0.0, 0.0, 0.0, 0.0, 0.0], 2, 8);

        List<string> outcomes = Run(options, 10, 0.0);

        Assert.That(
            outcomes,
            Is.EqualTo([
                "computed", "computed",
                "skipped", "skipped", "skipped", "skipped", "skipped", "skipped",
                "computed", "computed"
            ]));
    }

    [Test]
    public void RecomputeThresholdTest()
    {
        TeaCacheOptions options = new(1.0, [0.0, 0.0, 0.0, 0.0, 0.6], 2, 12);

        List<string> outcomes = Run(options, 10, 0.0);

        Assert.That(
            outcomes,
            Is.EqualTo([
                "computed", "computed",
                "skipped", "skipped",
                "computed", "computed",
                "skipped", "skipped",
                "computed", "computed"
            ]));
    }

    [Test]
    public void ReuseResidualTest()
    {
        TeaCacheOptions options = new(1.0, [0.0, 0.0, 0.0, 0.0, 0.0], 2, 8);

        using TeaCache cache = new(options);

        torch.Tensor last = torch.zeros([1, 8, 4], torch.ScalarType.Float32);

        for (int index = 0; index < 6; index++)
        {
            using torch.Tensor modulation = torch.full([1, 6, 4], 1.0, torch.ScalarType.Float32);
            using torch.Tensor working = torch.zeros([1, 8, 4], torch.ScalarType.Float32);

            last.Dispose();
            last = cache.Apply(working.alias(), modulation, input => input.add(3.0));
        }

        using torch.Tensor expected = torch.full([1, 8, 4], 3.0, torch.ScalarType.Float32);

        Assert.That(torch.allclose(last, expected), Is.True);

        last.Dispose();
    }
}
