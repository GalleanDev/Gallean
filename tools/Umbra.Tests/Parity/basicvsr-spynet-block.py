import os
import re
import sys

import torch

torch.set_num_threads(1)

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

from st_save import save_file

sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), "reference"))

from basicsr.archs.spynet_arch import SpyNet

WEIGHTS_URL = ("https://download.openmmlab.com/mmediting/restorers/basicvsr_plusplus/"
    "basicvsr_plusplus_c64n7_8x1_600k_reds4_20210217-db622b2f.pth")
OUT = sys.argv[1] if len(sys.argv) > 1 else os.path.join(
    os.path.dirname(os.path.abspath(__file__)), "basicvsr-spynet-block.safetensors")

raw = torch.hub.load_state_dict_from_url(WEIGHTS_URL, map_location="cpu", weights_only=False)["state_dict"]
state = {}
prefix = "generator.spynet."

for key, value in raw.items():
    if not key.startswith(prefix):
        continue

    name = key[len(prefix):]
    match = re.match(r"^(basic_module\.\d+\.basic_module\.)(\d+)\.conv\.(weight|bias)$", name)

    if match:
        name = f"{match.group(1)}{int(match.group(2)) * 2}.{match.group(3)}"

    state[name] = value

net = SpyNet(None)
net.load_state_dict(state, strict=True)
net.eval()

torch.manual_seed(7)
block_input = torch.rand(1, 8, 24, 20)

with torch.no_grad():
    block_output = net.basic_module[0](block_input)

print("block", tuple(block_output.shape), float(block_output.abs().max()))

save_file(
    {
        "blockInput": block_input,
        "blockOutput": block_output,
    },
    OUT,
)

print("wrote", OUT)
