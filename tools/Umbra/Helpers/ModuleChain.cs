using System.Globalization;
using TorchSharp;
using static TorchSharp.torch.nn;

namespace Umbra;

internal sealed class ModuleChain : Module<torch.Tensor, torch.Tensor>
{
    private readonly IReadOnlyList<Module<torch.Tensor, torch.Tensor>> stages;

    public ModuleChain(IReadOnlyList<Module<torch.Tensor, torch.Tensor>> stages)
        : base(nameof(ModuleChain))
    {
        this.stages = stages;

        for (int index = 0; index < stages.Count; index++)
        {
            register_module(index.ToString(CultureInfo.InvariantCulture), stages[index]);
        }
    }

    public override torch.Tensor forward(torch.Tensor input)
    {
        torch.Tensor result = input;

        foreach (Module<torch.Tensor, torch.Tensor> stage in stages)
        {
            torch.Tensor next = stage.call(result);

            if (!ReferenceEquals(result, input) && !ReferenceEquals(result, next))
            {
                result.Dispose();
            }

            result = next;
        }

        return result;
    }
}
