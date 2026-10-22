import os
import sys

import torch

torch.set_num_threads(1)

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

from st_save import save_file

sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), "reference"))

import fastdcn
import torchvision

OUT = sys.argv[1] if len(sys.argv) > 1 else os.path.join(
    os.path.dirname(os.path.abspath(__file__)), "basicvsr-deform-conv.safetensors")

torch.manual_seed(11)

batch, channels, height, width = 2, 32, 13, 11
out_channels, kernel, groups, padding, dilation = 8, 3, 4, 1, 1
taps = kernel * kernel

data = torch.rand(batch, channels, height, width)
offset = (torch.rand(batch, groups * 2 * taps, height, width) - 0.5) * 8.0
mask = torch.sigmoid(torch.rand(batch, groups * taps, height, width) * 4.0 - 2.0)
weight = torch.rand(out_channels, channels, kernel, kernel) - 0.5
bias = torch.rand(out_channels) - 0.5

with torch.no_grad():
    expected = torchvision.ops.deform_conv2d(
        data, offset, weight, bias,
        stride=(1, 1), padding=(padding, padding), dilation=(dilation, dilation), mask=mask)

with torch.no_grad():
    fast = fastdcn.deform_conv2d(
        data, offset, weight, bias,
        stride=(1, 1), padding=(padding, padding), dilation=(dilation, dilation), mask=mask)

print("expected", tuple(expected.shape), float(expected.abs().max()))
print("fastdcn vs torchvision", float((fast - expected).abs().max()))

save_file(
    {
        "input": data,
        "offset": offset,
        "mask": mask,
        "weight": weight,
        "bias": bias,
        "output": expected,
        "fastOutput": fast,
    },
    OUT,
)

print("wrote", OUT)
