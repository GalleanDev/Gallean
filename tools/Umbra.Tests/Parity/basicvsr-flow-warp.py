import os
import sys

import torch

torch.set_num_threads(1)

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

from st_save import save_file

sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), "reference"))

from basicsr.archs.arch_util import flow_warp

OUT = sys.argv[1] if len(sys.argv) > 1 else os.path.join(
    os.path.dirname(os.path.abspath(__file__)), "basicvsr-flow-warp.safetensors")

torch.manual_seed(7)
torch.rand(1, 8, 24, 20)
warp_planes = torch.rand(1, 5, 16, 12)
warp_flow = (torch.rand(1, 16, 12, 2) - 0.5) * 6.0

with torch.no_grad():
    warped_zeros = flow_warp(warp_planes, warp_flow, padding_mode="zeros")
    warped_border = flow_warp(warp_planes, warp_flow, padding_mode="border")

print("warp", tuple(warped_zeros.shape), float(warped_zeros.abs().max()))

save_file(
    {
        "warpPlanes": warp_planes,
        "warpFlow": warp_flow,
        "warpedZeros": warped_zeros,
        "warpedBorder": warped_border,
    },
    OUT,
)

print("wrote", OUT)
