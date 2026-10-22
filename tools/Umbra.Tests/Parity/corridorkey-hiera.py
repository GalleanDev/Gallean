import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import torch

torch.set_num_threads(1)

REPO = os.environ.get("CORRIDORKEY_REPO")

if REPO is None:
    raise SystemExit("Set CORRIDORKEY_REPO to a checkout of nikopueringer/CorridorKey.")

sys.path.insert(0, os.path.join(REPO, ".venv", "lib", "python3.13", "site-packages"))

from timm.layers import get_norm_layer
from timm.models.hiera import Hiera, MaskUnitAttention, HieraBlock, Reroll, Unroll

from st_save import save_file

OUT = sys.argv[1] if len(sys.argv) > 1 else os.path.join(
    os.path.dirname(os.path.abspath(__file__)), "corridorkey-hiera.safetensors")

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
GRID = IMAGE // PATCH_STRIDE[0]

torch.manual_seed(17)

tensors = {}


def randomize(module, scale=0.2):
    with torch.no_grad():
        for parameter in module.parameters():
            parameter.copy_(torch.randn_like(parameter).mul(scale))


def dump_weights(prefix, module, keep=None):
    for name, parameter in module.named_parameters():
        if keep is None or name.startswith(keep):
            tensors[f"{prefix}.weights.{name}"] = parameter.data


def build_hiera():
    model = Hiera(
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
    model.eval()
    randomize(model)

    return model


hiera = build_hiera()

for prefix in ("patch_embed", "pos_embed", "blocks"):
    dump_weights("hiera", hiera, keep=prefix)

image = torch.randn(BATCH, CHANNELS, IMAGE, IMAGE)
tensors["hiera.input"] = image

with torch.no_grad():
    embedded = hiera.patch_embed(image)
    tensors["stem.expected"] = hiera.unroll(hiera._pos_embed(embedded))
    features = hiera.forward_intermediates(image, output_fmt="NCHW", intermediates_only=True)

for index, feature in enumerate(features):
    tensors[f"hiera.expected{index}"] = feature

unroll = Unroll((IMAGE, IMAGE), PATCH_STRIDE, [(2, 2)] * (len(STAGES) - 1))
unroll_input = torch.randn(BATCH, GRID * GRID, EMBED)
tensors["unroll.input"] = unroll_input

with torch.no_grad():
    tensors["unroll.expected"] = unroll(unroll_input)

reroll = Reroll(
    (IMAGE, IMAGE),
    PATCH_STRIDE,
    [(2, 2)] * (len(STAGES) - 1),
    [sum(STAGES[:i]) - 1 for i in range(1, len(STAGES) + 1)],
    Q_POOL,
)

for stage, block_index in enumerate([sum(STAGES[:i]) - 1 for i in range(1, len(STAGES) + 1)]):
    tokens = GRID * GRID // (4 ** stage)
    channels = EMBED * (2 ** stage)
    reroll_input = torch.randn(BATCH, tokens, channels)
    tensors[f"reroll{stage}.input"] = reroll_input

    with torch.no_grad():
        tensors[f"reroll{stage}.expected"] = reroll(reroll_input, block_index)


def dump_attention(name, dim, dim_out, heads, q_stride, window_size, use_mask_unit_attn, tokens):
    attention = MaskUnitAttention(dim, dim_out, heads, q_stride, window_size, use_mask_unit_attn)
    attention.eval()
    randomize(attention)
    dump_weights(name, attention)
    attention_input = torch.randn(BATCH, tokens, dim)
    tensors[f"{name}.input"] = attention_input

    with torch.no_grad():
        tensors[f"{name}.expected"] = attention(attention_input)


dump_attention("windowed", 8, 8, 1, 1, 64, True, 256)
dump_attention("windowedPooled", 8, 16, 2, 4, 16, True, 256)
dump_attention("global", 32, 32, 4, 1, 4, False, 16)
dump_attention("globalPooled", 32, 64, 8, 4, 1, False, 16)


def dump_block(name, dim, dim_out, heads, q_stride, window_size, use_mask_unit_attn, tokens):
    block = HieraBlock(
        dim=dim,
        dim_out=dim_out,
        heads=heads,
        q_stride=q_stride,
        window_size=window_size,
        use_mask_unit_attn=use_mask_unit_attn,
        norm_layer=get_norm_layer("LayerNorm"),
    )
    block.eval()
    randomize(block)
    dump_weights(name, block)
    block_input = torch.randn(BATCH, tokens, dim)
    tensors[f"{name}.input"] = block_input

    with torch.no_grad():
        tensors[f"{name}.expected"] = block(block_input)


dump_block("plainBlock", 8, 8, 1, 1, 64, True, 256)
dump_block("expandingBlock", 8, 16, 2, 4, 16, True, 256)
dump_block("globalBlock", 32, 64, 8, 4, 1, False, 16)

save_file(tensors, OUT)
print(f"wrote {len(tensors)} tensors to {OUT}")
