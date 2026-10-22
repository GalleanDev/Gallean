import re
import os
import sys

import torch

torch.set_num_threads(1)

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

from st_save import save_file

sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), "reference"))

import fastdcn

fastdcn.patch()

from basicsr.archs.basicvsrpp_arch import BasicVSRPlusPlus

WEIGHTS_URL = ("https://download.openmmlab.com/mmediting/restorers/basicvsr_plusplus/"
    "basicvsr_plusplus_c64n7_8x1_600k_reds4_20210217-db622b2f.pth")
OUT = sys.argv[1] if len(sys.argv) > 1 else os.path.join(
    os.path.dirname(os.path.abspath(__file__)), "basicvsr-network.safetensors")

raw = torch.hub.load_state_dict_from_url(WEIGHTS_URL, map_location="cpu", weights_only=False)["state_dict"]
state = {}

for key, value in raw.items():
    if not key.startswith("generator."):
        continue

    name = key[len("generator."):]
    name = name.replace("upsample1.upsample_conv", "upconv1").replace("upsample2.upsample_conv", "upconv2")
    match = re.match(r"^(spynet\.basic_module\.\d+\.basic_module\.)(\d+)\.conv\.(weight|bias)$", name)

    if match:
        name = f"{match.group(1)}{int(match.group(2)) * 2}.{match.group(3)}"

    state[name] = value

model = BasicVSRPlusPlus(mid_channels=64, num_blocks=7, spynet_path=None)
model.load_state_dict(state, strict=True)
model.eval()
model.cpu_cache = False

assert model.is_with_alignment

torch.manual_seed(31)
clip = torch.rand(1, 3, 3, 64, 64)

with torch.no_grad():
    output = model(clip)

print("output", tuple(output.shape), float(output.abs().max()))

save_file(
    {"clip": clip, "output": output},
    OUT)

print("wrote", OUT)
