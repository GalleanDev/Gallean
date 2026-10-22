using TorchSharp;

namespace Umbra.Tests;

public sealed class WanCausalConv3dTests
{
    [Test]
    public void ShapeTest()
    {
        using WanCausalConv3d conv = new(3, 8, (3, 3, 3), (1, 1, 1));
        using torch.Tensor input = torch.zeros([1, 3, 5, 16, 16]);

        using torch.Tensor result = conv.call(input);

        Assert.That(result.shape, Is.EqualTo(new long[] { 1, 8, 5, 16, 16 }));
    }

    [Test]
    public void CausalTest()
    {
        using WanCausalConv3d conv = new(1, 1, (3, 1, 1), (1, 0, 0));

        conv.load_state_dict(new Dictionary<string, torch.Tensor>
        {
            ["weight"] = torch.ones([1, 1, 3, 1, 1]),
            ["bias"] = torch.zeros(1)
        });

        using torch.Tensor input = torch.zeros([1, 1, 4, 1, 1]);
        input[0, 0, 2, 0, 0] = 1.0f;

        using torch.Tensor result = conv.call(input);

        Assert.That(result.reshape(-1).data<float>().ToArray(), Is.EqualTo([0.0f, 0.0f, 1.0f, 1.0f]));
    }
}
