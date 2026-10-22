using TorchSharp;

namespace Umbra.Tests;

public sealed class BasicVsrConvResidualBlocksTests
{
    private static List<string> FormatStateDict(torch.nn.Module module)
    {
        List<string> rows = [];

        foreach (KeyValuePair<string, torch.Tensor> entry in module.state_dict())
        {
            rows.Add($"{entry.Key} [{string.Join(",", entry.Value.shape)}]");
        }

        rows.Sort(StringComparer.Ordinal);

        return rows;
    }

    [Test]
    public void StateDictTest()
    {
        using BasicVsrConvResidualBlocks blocks = new(128, 64, 2);

        Assert.That(FormatStateDict(blocks), Is.EqualTo([
            "main.0.bias [64]",
            "main.0.weight [64,128,3,3]",
            "main.2.0.conv1.bias [64]",
            "main.2.0.conv1.weight [64,64,3,3]",
            "main.2.0.conv2.bias [64]",
            "main.2.0.conv2.weight [64,64,3,3]",
            "main.2.1.conv1.bias [64]",
            "main.2.1.conv1.weight [64,64,3,3]",
            "main.2.1.conv2.bias [64]",
            "main.2.1.conv2.weight [64,64,3,3]"
        ]));
    }

    [Test]
    public void ForwardTest()
    {
        using BasicVsrConvResidualBlocks blocks = new(6, 4, 1);
        using torch.Tensor input = torch.ones(2, 6, 8, 5);
        using torch.Tensor output = blocks.call(input);

        Assert.That(output.shape, Is.EqualTo(new long[] { 2, 4, 8, 5 }));
    }

    [Test]
    public void ResidualSkipTest()
    {
        using BasicVsrResidualBlock block = new(2);
        using torch.Tensor input = torch.full([1, 2, 3, 3], 0.25);

        using (torch.no_grad())
        {
            foreach (KeyValuePair<string, torch.Tensor> entry in block.state_dict())
            {
                entry.Value.zero_();
            }
        }

        using torch.Tensor output = block.call(input);

        Assert.That(output.shape, Is.EqualTo(new long[] { 1, 2, 3, 3 }));
        Assert.That(output.sum().ToSingle(), Is.EqualTo(4.5f));
    }
}
