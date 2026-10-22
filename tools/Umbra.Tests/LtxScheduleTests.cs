using TorchSharp;

namespace Umbra.Tests;

public sealed class LtxScheduleTests
{
    [Test]
    public void SigmasTest()
    {
        Dictionary<string, torch.Tensor> parity = TestParity.Load("ltx-schedule");

        LtxSchedule schedule = LtxSchedule.Detailing;
        float[] sigmas = parity["schedule.sigmas"].data<float>().ToArray();

        Assert.That(schedule.Steps, Is.EqualTo(3));
        Assert.That(sigmas, Is.EqualTo([0.909375f, 0.725f, 0.421875f, 0.0f]));
        Assert.That(schedule.GetSigma(0), Is.EqualTo(0.909375));
        Assert.That(schedule.GetSigma(3), Is.Zero);
    }

    [Test]
    public void DenoiseTest()
    {
        Dictionary<string, torch.Tensor> parity = TestParity.Load("ltx-schedule");

        using torch.Tensor result = LtxSchedule.Detailing.Denoise(
            parity["step.sample"], parity["step.velocity"], parity["step.timesteps"]);

        Assert.That(TestHelpers.MaxDifference(result, parity["step.denoised"]), Is.LessThan(1e-5f));
    }

    [Test]
    public void StepTest()
    {
        Dictionary<string, torch.Tensor> parity = TestParity.Load("ltx-schedule");

        using torch.Tensor result = LtxSchedule.Detailing.Step(parity["step.sample"], parity["step.blended"], 1);

        Assert.That(TestHelpers.MaxDifference(result, parity["step.expected"]), Is.LessThan(1e-5f));
    }

    [Test]
    public void LoraFuseTest()
    {
        Dictionary<string, torch.Tensor> parity = TestParity.Load("ltx-schedule");

        using torch.Tensor result = LtxLora.Fuse(parity["lora.weight"], parity["lora.down"], parity["lora.up"], 0.5);

        Assert.That(TestHelpers.MaxDifference(result, parity["lora.expected"]), Is.LessThan(1e-5f));
    }

    [Test]
    public void PositionsTest()
    {
        Dictionary<string, torch.Tensor> parity = TestParity.Load("ltx-dit");

        using torch.Tensor target = LtxPositions.Build(2, 2, 3, 8, 32, 1, 12.0, torch.CPU);
        using torch.Tensor reference = LtxPositions.Build(2, 1, 2, 8, 32, 2, 12.0, torch.CPU);
        using torch.Tensor targetBatch = target.expand(2, -1, -1, -1);
        using torch.Tensor referenceBatch = reference.expand(2, -1, -1, -1);
        using torch.Tensor combined = torch.cat([targetBatch, referenceBatch], dim: 2);

        Assert.That(combined.shape, Is.EqualTo(parity["dit.positions"].shape));
        Assert.That(TestHelpers.MaxDifference(combined, parity["dit.positions"]), Is.LessThan(1e-5f));
    }
}
