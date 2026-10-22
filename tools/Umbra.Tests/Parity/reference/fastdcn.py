import torch
import torchvision

_original = torchvision.ops.deform_conv2d


def deform_conv2d(input, offset, weight, bias=None, stride=(1, 1), padding=(0, 0),
                  dilation=(1, 1), mask=None):
    if mask is None or input.is_cuda or stride != (1, 1):
        return _original(input, offset, weight, bias, stride, padding, dilation, mask)

    N, C, H, W = input.shape
    out_channels, _, kh, kw = weight.shape
    taps = kh * kw
    groups = offset.shape[1] // (2 * taps)

    ys = torch.arange(H, dtype=input.dtype).view(1, H, 1)
    xs = torch.arange(W, dtype=input.dtype).view(1, 1, W)
    ky = torch.arange(kh, dtype=input.dtype).view(kh, 1, 1, 1) * dilation[0] - padding[0]
    kx = torch.arange(kw, dtype=input.dtype).view(1, kw, 1, 1) * dilation[1] - padding[1]

    offsets = offset.view(N, groups, taps, 2, H, W)
    py = (ys + ky).expand(kh, kw, H, W).reshape(1, 1, taps, H, W) + offsets[:, :, :, 0]
    px = (xs + kx).expand(kh, kw, H, W).reshape(1, 1, taps, H, W) + offsets[:, :, :, 1]

    grid = torch.stack([2.0 * px / max(W - 1, 1) - 1.0,
                        2.0 * py / max(H - 1, 1) - 1.0], dim=-1)
    grid = grid.reshape(N * groups, taps * H, W, 2)

    sampled = torch.nn.functional.grid_sample(
        input.reshape(N * groups, C // groups, H, W), grid,
        mode="bilinear", padding_mode="zeros", align_corners=True)
    sampled = sampled.view(N, groups, C // groups, taps, H, W)
    sampled = sampled * mask.view(N, groups, 1, taps, H, W)
    columns = sampled.permute(0, 1, 3, 2, 4, 5).reshape(N, C * taps, H, W)

    packed = weight.view(out_channels, groups, C // groups, taps)
    packed = packed.permute(0, 1, 3, 2).reshape(out_channels, C * taps, 1, 1)

    return torch.nn.functional.conv2d(columns, packed, bias)


def patch():
    torchvision.ops.deform_conv2d = deform_conv2d
