using TorchSharp;
using TorchSharp.Modules;
using static TorchSharp.torch.nn;

namespace Umbra;

internal static class ModuleParameters
{
    public static void Load(Module module, Dictionary<string, torch.Tensor> state)
    {
        Load(module, state, torch.get_default_device());
    }

    public static void Load(Module module, Dictionary<string, torch.Tensor> state, torch.Device device)
    {
        foreach ((string path, Module owner) in ListModules(module))
        {
            List<(string Name, Parameter Parameter)> declared = [.. owner.named_parameters(recurse: false)];

            foreach ((string name, Parameter parameter) in declared)
            {
                string qualifiedName = QualifyName(path, name);

                torch.Tensor tensor = Require(state, qualifiedName);

                if (!parameter.shape.SequenceEqual(tensor.shape))
                {
                    throw new InvalidDataException($"The model entry '{qualifiedName}' has an unexpected shape.");
                }

                Parameter value = new(tensor.to(device), requires_grad: false);

                state.Remove(qualifiedName);
                tensor.Dispose();
                SetParameter(owner, name, value);
            }
        }

        if (state.Count > 0)
        {
            throw new InvalidDataException($"The model has unexpected entries: {string.Join(", ", state.Keys)}.");
        }
    }

    public static void Attach(Module module, IReadOnlyDictionary<string, torch.Tensor> weights)
    {
        foreach ((string path, Module owner) in ListModules(module))
        {
            List<(string Name, Parameter Parameter)> declared = [.. owner.named_parameters(recurse: false)];

            foreach ((string name, Parameter _) in declared)
            {
                string qualifiedName = QualifyName(path, name);

                Parameter value = new(Require(weights, qualifiedName).alias(), requires_grad: false);

                value.DetachFromDisposeScope();
                SetParameter(owner, name, value);
            }
        }
    }

    public static IReadOnlySet<string> ListParameterNames(Module module)
    {
        HashSet<string> names = [];

        foreach ((string path, Module owner) in ListModules(module))
        {
            foreach ((string name, Parameter _) in owner.named_parameters(recurse: false))
            {
                names.Add(QualifyName(path, name));
            }
        }

        return names;
    }

    public static void MoveTo(Module module, torch.ScalarType type, torch.Device device)
    {
        using (torch.no_grad())
        {
            foreach ((string _, Module owner) in ListModules(module))
            {
                List<(string Name, Parameter Parameter)> declared = [.. owner.named_parameters(recurse: false)];

                foreach ((string name, Parameter parameter) in declared)
                {
                    if (parameter.dtype != type ||
                        parameter.device_type != device.type ||
                        parameter.device_index != device.index)
                    {
                        Parameter moved = new(parameter.to(type, device), requires_grad: false);

                        moved.DetachFromDisposeScope();
                        SetParameter(owner, name, moved);
                    }
                }
            }
        }
    }

    private static torch.Tensor Require(IReadOnlyDictionary<string, torch.Tensor> state, string name)
    {
        if (!state.TryGetValue(name, out torch.Tensor? tensor))
        {
            throw new InvalidDataException($"The model is missing '{name}'.");
        }

        return tensor;
    }

    private static IReadOnlyList<(string Path, Module Owner)> ListModules(Module module)
    {
        return [(string.Empty, module), .. module.named_modules()];
    }

    private static string QualifyName(string path, string name)
    {
        if (path.Length > 0)
        {
            return $"{path}.{name}";
        }
        else
        {
            return name;
        }
    }

    private static void SetParameter(Module owner, string name, Parameter value)
    {
        if (owner is IParameterModule custom)
        {
            custom.SetParameter(name, value);
            return;
        }

        switch (owner, name)
        {
            case (Conv2d leaf, "weight"):
                leaf.weight = value;
                break;

            case (Conv2d leaf, "bias"):
                leaf.bias = value;
                break;

            case (Conv3d leaf, "weight"):
                leaf.weight = value;
                break;

            case (Conv3d leaf, "bias"):
                leaf.bias = value;
                break;

            case (Linear leaf, "weight"):
                leaf.weight = value;
                break;

            case (Linear leaf, "bias"):
                leaf.bias = value;
                break;

            case (GroupNorm leaf, "weight"):
                leaf.weight = value;
                break;

            case (GroupNorm leaf, "bias"):
                leaf.bias = value;
                break;

            case (LayerNorm leaf, "weight"):
                leaf.weight = value;
                break;

            case (LayerNorm leaf, "bias"):
                leaf.bias = value;
                break;

            case (Embedding leaf, "weight"):
                leaf.weight = value;
                break;

            default:
                throw new NotSupportedException($"The parameter '{name}' of the '{owner.GetName()}' module is not supported.");
        }
    }
}
