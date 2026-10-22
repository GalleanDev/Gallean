using TorchSharp;

namespace Umbra.Tests;

public sealed class SurfaceFlowMatchSchedulerTests
{
    [Test]
    public void ScheduleTest()
    {
        SurfaceFlowMatchScheduler scheduler = new(50);

        Assert.That(scheduler.StepCount, Is.EqualTo(50));
        Assert.That(scheduler.GetSigma(0), Is.EqualTo(1.0));
        Assert.That(scheduler.GetSigma(1), Is.EqualTo(0.9959349593495935));

        Assert.That(scheduler.GetSigma(49), Is.EqualTo(0.09259259259259267));
        Assert.That(scheduler.GetSigma(50), Is.Zero);
        Assert.That(scheduler.GetTimestep(1), Is.EqualTo(995.9349593495936));
    }

    [Test]
    public void StepTest()
    {
        SurfaceFlowMatchScheduler scheduler = new(50);
        using torch.Tensor sample = torch.ones(2);
        using torch.Tensor estimate = torch.full(2, 4.0f);

        using torch.Tensor result = scheduler.Step(estimate, sample, 0);

        Assert.That(result.data<float>().ToArray(), Is.EqualTo([0.98373985f, 0.98373985f]));
    }

    [Test]
    public void ScheduleParityTest()
    {
        Dictionary<string, torch.Tensor> dump = TestParity.Load("wan-schedule");

        SurfaceFlowMatchScheduler scheduler = new(50);

        Assert.That(scheduler.StepCount, Is.EqualTo(dump["sigmas"].shape[0]));

        for (int step = 0; step < scheduler.StepCount; step++)
        {
            Assert.That(scheduler.GetSigma(step), Is.EqualTo(dump["sigmas"][step].ToDouble()).Within(1e-6));
            Assert.That(scheduler.GetTimestep(step), Is.EqualTo(dump["timesteps"][step].ToDouble()).Within(1e-3));
        }
    }
}
