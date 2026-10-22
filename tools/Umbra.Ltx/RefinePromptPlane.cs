using TorchSharp;

namespace Umbra.Ltx;

internal sealed record RefinePromptPlane(torch.Tensor Plane, IReadOnlyList<torch.Tensor> Contexts);
