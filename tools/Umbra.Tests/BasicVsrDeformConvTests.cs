using TorchSharp;

namespace Umbra.Tests;

public sealed class BasicVsrDeformConvTests
{
    private static torch.Tensor Apply(Dictionary<string, torch.Tensor> dump)
    {
        return BasicVsrDeformConv.Apply(
            dump["input"],
            dump["offset"],
            dump["mask"],
            dump["weight"],
            dump["bias"],
            padding: 1,
            dilation: 1);
    }

    [Test]
    public void ShapeTest()
    {
        using torch.Tensor input = torch.ones(2, 8, 5, 4);
        using torch.Tensor offset = torch.zeros(2, 2 * 2 * 9, 5, 4);
        using torch.Tensor mask = torch.ones(2, 2 * 9, 5, 4);
        using torch.Tensor weight = torch.ones(3, 8, 3, 3);
        using torch.Tensor bias = torch.zeros(3);

        using (torch.no_grad())
        {
            using torch.Tensor output = BasicVsrDeformConv.Apply(input, offset, mask, weight, bias, padding: 1, dilation: 1);

            Assert.That(output.shape, Is.EqualTo(new long[] { 2, 3, 5, 4 }));
        }
    }

    [Test]
    public void FastPathParityTest()
    {
        Dictionary<string, torch.Tensor> dump = TestParity.Load("basicvsr-deform-conv");

        using (torch.no_grad())
        {
            using torch.Tensor actual = Apply(dump);

            Assert.That(TestHelpers.MaxDifference(actual, dump["fastOutput"]), Is.LessThan(1e-4f));
        }
    }

    [Test]
    public void TorchvisionParityTest()
    {
        Dictionary<string, torch.Tensor> dump = TestParity.Load("basicvsr-deform-conv");

        using (torch.no_grad())
        {
            using torch.Tensor actual = Apply(dump);

            Assert.That(TestHelpers.MaxDifference(actual, dump["output"]), Is.LessThan(1e-4f));
        }
    }
}
