using System.Runtime.CompilerServices;
using TorchSharp.Modules;
using static TorchSharp.torch.nn;

namespace Umbra;

internal static class ModuleExtensions
{
    extension(Module module)
    {
        public Parameter ReplaceParameter(Parameter? current, string name, Parameter value)
        {
            current?.Dispose();
            value.DetachFromDisposeScope();
            ConditionallyRegisterParameter(module, name, value);

            return value;
        }
    }

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "ConditionallyRegisterParameter")]
    private static extern void ConditionallyRegisterParameter(Module module, string name, Parameter? value);
}
