import os
import sys

import torch

torch.set_num_threads(1)

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

from st_save import save_file

sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), "reference"))

import fastdcn

fastdcn.patch()

from basicsr.archs.basicvsrpp_arch import SecondOrderDeformableAlignment

WEIGHTS_URL = ("https://download.openmmlab.com/mmediting/restorers/basicvsr_plusplus/"
    "basicvsr_plusplus_c64n7_8x1_600k_reds4_20210217-db622b2f.pth")
OUT = sys.argv[1] if len(sys.argv) > 1 else os.path.join(
    os.path.dirname(os.path.abspath(__file__)), "basicvsr-deform-align.safetensors")

raw = torch.hub.load_state_dict_from_url(WEIGHTS_URL, map_location="cpu", weights_only=False)["state_dict"]
prefix = "generator.deform_align.backward_1."
state = {k[len(prefix):]: v for k, v in raw.items() if k.startswith(prefix)}

print("keys", sorted(state))

module = SecondOrderDeformableAlignment(
    128, 64, 3, padding=1, deformable_groups=16, max_residue_magnitude=10)
module.load_state_dict(state, strict=True)
module.eval()

torch.manual_seed(23)
features = torch.rand(1, 128, 20, 16) - 0.5
condition = torch.rand(1, 192, 20, 16) - 0.5
first_flow = (torch.rand(1, 2, 20, 16) - 0.5) * 4.0
second_flow = (torch.rand(1, 2, 20, 16) - 0.5) * 4.0

with torch.no_grad():
    output = module(features, condition, first_flow, second_flow)

print("output", tuple(output.shape), float(output.abs().max()))

save_file(
    {
        "features": features,
        "condition": condition,
        "firstFlow": first_flow,
        "secondFlow": second_flow,
        "output": output,
    },
    OUT,
)

print("wrote", OUT)
