using TorchSharp.Modules;

namespace Umbra;

internal interface IParameterModule
{
    void SetParameter(string name, Parameter value);
}
