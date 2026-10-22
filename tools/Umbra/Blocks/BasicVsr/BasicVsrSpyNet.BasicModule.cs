using TorchSharp;
using TorchSharp.Utils;
using static TorchSharp.torch.nn;

namespace Umbra;

internal sealed partial class BasicVsrSpyNet
{
    private sealed class BasicModule : Module<torch.Tensor, torch.Tensor>
    {
        [ComponentName(Name = "basic_module")]
        private readonly ModuleChain basicModule;

        public BasicModule()
            : base(nameof(BasicModule))
        {
            basicModule = new ModuleChain(
            [
                Conv2d(8, 32, kernel_size: 7, stride: 1, padding: 3),
                ReLU(inplace: false),
                Conv2d(32, 64, kernel_size: 7, stride: 1, padding: 3),
                ReLU(inplace: false),
                Conv2d(64, 32, kernel_size: 7, stride: 1, padding: 3),
                ReLU(inplace: false),
                Conv2d(32, 16, kernel_size: 7, stride: 1, padding: 3),
                ReLU(inplace: false),
                Conv2d(16, 2, kernel_size: 7, stride: 1, padding: 3)
            ]);

            RegisterComponents();
        }

        public override torch.Tensor forward(torch.Tensor input)
        {
            return basicModule.call(input);
        }
    }
}
