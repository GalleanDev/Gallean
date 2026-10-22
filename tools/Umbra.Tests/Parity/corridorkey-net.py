import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import torch
import torch.nn as nn

torch.set_num_threads(1)

REPO = os.environ.get("CORRIDORKEY_REPO")

if REPO is None:
    raise SystemExit("Set CORRIDORKEY_REPO to a checkout of nikopueringer/CorridorKey.")

sys.path.insert(0, os.path.join(REPO, ".venv", "lib", "python3.13", "site-packages"))
sys.path.insert(0, REPO)

from timm.models.hiera import Hiera

from CorridorKeyModule.core.model_transformer import CNNRefinerModule, DecoderHead, GreenFormer, RefinerBlock

from st_save import save_file

OUT = sys.argv[1] if len(sys.argv) > 1 else os.path.join(
    os.path.dirname(os.path.abspath(__file__)), "corridorkey-net.safetensors")

BATCH = 2
IMAGE = 32
CHANNELS = 4
EMBED = 8
HEADS = 1
STAGES = (2, 2, 2, 2)
Q_POOL = 3
MASK_UNIT = (8, 8)
PATCH_KERNEL = (3, 3)
PATCH_STRIDE = (2, 2)
PATCH_PADDING = (1, 1)
FEATURE_CHANNELS = [EMBED * (2 ** stage) for stage in range(len(STAGES))]
DECODER = 32
REFINER = 16

torch.manual_seed(23)

tensors = {}


def randomize(module, scale=0.2):
    with torch.no_grad():
        for parameter in module.parameters():
            parameter.copy_(torch.randn_like(parameter).mul(scale))

        for name, buffer in module.named_buffers():
            if name.endswith("running_mean"):
                buffer.copy_(torch.randn_like(buffer).mul(scale))
            elif name.endswith("running_var"):
                buffer.copy_(torch.rand_like(buffer).add(0.5))


def dump_state(prefix, module):
    for name, tensor in module.state_dict().items():
        if name.endswith("num_batches_tracked"):
            continue

        tensors[f"{prefix}.weights.{name.replace('encoder.model.', 'encoder.')}"] = tensor


class ToyEncoder(nn.Module):
    def __init__(self):
        super().__init__()
        self.model = Hiera(
            img_size=(IMAGE, IMAGE),
            in_chans=CHANNELS,
            embed_dim=EMBED,
            num_heads=HEADS,
            num_classes=2,
            stages=STAGES,
            q_pool=Q_POOL,
            mask_unit_size=MASK_UNIT,
            patch_kernel=PATCH_KERNEL,
            patch_stride=PATCH_STRIDE,
            patch_padding=PATCH_PADDING,
        )
        del self.model.head

    def forward(self, x):
        return self.model.forward_intermediates(x, output_fmt="NCHW", intermediates_only=True)


refinerBlock = RefinerBlock(REFINER, dilation=2)
refinerBlock.eval()
randomize(refinerBlock)
dump_state("refinerBlock", refinerBlock)
refinerBlockInput = torch.randn(BATCH, REFINER, 8, 8)
tensors["refinerBlock.input"] = refinerBlockInput

with torch.no_grad():
    tensors["refinerBlock.expected"] = refinerBlock(refinerBlockInput)

cnnRefiner = CNNRefinerModule(in_channels=7, hidden_channels=REFINER, out_channels=4)
cnnRefiner.eval()
randomize(cnnRefiner)
dump_state("cnnRefiner", cnnRefiner)
cnnColors = torch.randn(BATCH, 3, 16, 16)
cnnCoarse = torch.rand(BATCH, 4, 16, 16)
tensors["cnnRefiner.colors"] = cnnColors
tensors["cnnRefiner.coarse"] = cnnCoarse

with torch.no_grad():
    tensors["cnnRefiner.expected"] = cnnRefiner(cnnColors, cnnCoarse)

decoderHead = DecoderHead(FEATURE_CHANNELS, DECODER, output_dim=3)
decoderHead.eval()
randomize(decoderHead)
dump_state("decoderHead", decoderHead)
decoderFeatures = [
    torch.randn(BATCH, FEATURE_CHANNELS[stage], 16 >> stage, 16 >> stage) for stage in range(len(STAGES))
]

for stage, feature in enumerate(decoderFeatures):
    tensors[f"decoderHead.input{stage}"] = feature

with torch.no_grad():
    tensors["decoderHead.expected"] = decoderHead(decoderFeatures)

former = GreenFormer.__new__(GreenFormer)
nn.Module.__init__(former)
former.encoder = ToyEncoder()
former.alpha_decoder = DecoderHead(FEATURE_CHANNELS, DECODER, output_dim=1)
former.fg_decoder = DecoderHead(FEATURE_CHANNELS, DECODER, output_dim=3)
former.use_refiner = True
former.refiner = CNNRefinerModule(in_channels=7, hidden_channels=REFINER, out_channels=4)
former.eval()
randomize(former)
dump_state("former", former)

formerInput = torch.randn(BATCH, CHANNELS, IMAGE, IMAGE)
tensors["former.input"] = formerInput

with torch.no_grad():
    prediction = former(formerInput)

tensors["former.expectedAlpha"] = prediction["alpha"]
tensors["former.expectedForeground"] = prediction["fg"]

save_file(tensors, OUT)
print(f"wrote {len(tensors)} tensors to {OUT}")
