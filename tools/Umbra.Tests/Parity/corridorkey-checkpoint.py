import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import torch
import torch.nn.functional as F
import torchvision
import torchvision.transforms.v2.functional as TF
from safetensors.torch import load_file

torch.set_num_threads(os.cpu_count())

REPO = os.environ.get("CORRIDORKEY_REPO")

if REPO is None:
    raise SystemExit("Set CORRIDORKEY_REPO to a checkout of nikopueringer/CorridorKey.")

sys.path.insert(0, REPO)

from CorridorKeyModule.core.model_transformer import GreenFormer

from st_save import save_file

if len(sys.argv) < 2:
    raise SystemExit("Usage: corridorkey-checkpoint.py <checkpoint.safetensors> [out.safetensors] [image size]")

CHECKPOINT = sys.argv[1]
OUT = sys.argv[2] if len(sys.argv) > 2 else os.path.join(
    os.path.dirname(os.path.abspath(__file__)), "corridorkey-checkpoint.safetensors")
IMAGE_SIZE = int(sys.argv[3]) if len(sys.argv) > 3 else 512

HEIGHT = 300
WIDTH = 220
MEAN = [0.485, 0.456, 0.406]
STD = [0.229, 0.224, 0.225]

torch.manual_seed(5)

model = GreenFormer(img_size=IMAGE_SIZE)
model.eval()

target = model.state_dict()
state = {}

for name, tensor in load_file(CHECKPOINT, device="cpu").items():
    if name.startswith("_orig_mod."):
        name = name[10:]

    if "pos_embed" in name and name in target and tensor.shape != target[name].shape:
        source = int(math.sqrt(tensor.shape[1]))
        grid = int(math.sqrt(target[name].shape[1]))
        planes = tensor.permute(0, 2, 1).view(1, tensor.shape[2], source, source)
        resized = F.interpolate(planes, size=(grid, grid), mode="bicubic", align_corners=False)
        tensor = resized.flatten(2).transpose(1, 2)

    state[name] = tensor

missing, unexpected = model.load_state_dict(state, strict=False)

if len(missing) > 0 or len(unexpected) > 0:
    raise SystemExit(f"checkpoint does not match the model: missing {missing}, unexpected {unexpected}")

colors = torch.rand(1, 3, HEIGHT, WIDTH)
hint = torch.rand(1, 1, HEIGHT, WIDTH)

with torch.no_grad():
    scaled = TF.resize(colors, [IMAGE_SIZE, IMAGE_SIZE], interpolation=torchvision.transforms.InterpolationMode.BILINEAR)
    scaledHint = TF.resize(hint, [IMAGE_SIZE, IMAGE_SIZE], interpolation=torchvision.transforms.InterpolationMode.BILINEAR)
    prepared = torch.concat((TF.normalize(scaled, MEAN, STD), scaledHint), -3)
    prediction = model(prepared)

save_file({
    "colors": colors,
    "hint": hint,
    "alpha": prediction["alpha"],
    "foreground": prediction["fg"],
}, OUT)

print(f"wrote 4 tensors at {IMAGE_SIZE} to {OUT}")
