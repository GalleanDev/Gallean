using TorchSharp;

namespace Umbra.Ltx;

internal sealed record LtxLoraEntry(torch.Tensor Down, torch.Tensor Up);
