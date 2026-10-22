import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import torch

torch.set_num_threads(1)

REPO = os.environ.get("LTX2_REPO")

if REPO is None:
    raise SystemExit("Set LTX2_REPO to a checkout of Lightricks/LTX-2.")

sys.path.insert(0, os.path.join(REPO, "packages", "ltx-core", "src"))

from ltx_core.components.patchifiers import VideoLatentPatchifier, get_pixel_coords
from ltx_core.model.transformer.adaln import AdaLayerNormSingle
from ltx_core.model.transformer.attention import Attention
from ltx_core.model.transformer.feed_forward import FeedForward
from ltx_core.model.transformer.rope import LTXRopeType, apply_rotary_emb, generate_freq_grid_np, precompute_freqs_cis
from ltx_core.model.transformer.transformer import BasicAVTransformerBlock, TransformerConfig
from ltx_core.model.transformer.transformer_args import TransformerArgs
from ltx_core.types import SpatioTemporalScaleFactors, VideoLatentShape

from st_save import save_file

OUT = sys.argv[1] if len(sys.argv) > 1 else os.path.join(
    os.path.dirname(os.path.abspath(__file__)), "ltx-layers.safetensors")

HIDDEN = 16
HEADS = 2
HEAD_DIM = 8
CONTEXT = 16
CONTEXT_LENGTH = 5
BATCH = 2
FRAMES = 2
HEIGHT = 2
WIDTH = 3
FPS = 12.0
THETA = 10000.0
MAX_POSITIONS = [20, 2048, 2048]

torch.manual_seed(11)

tensors = {}

def randomize(module, scale=0.2):
    with torch.no_grad():
        for parameter in module.parameters():
            parameter.copy_(torch.randn_like(parameter).mul(scale))

def dump_weights(prefix, module):
    for name, parameter in module.named_parameters():
        tensors[f"{prefix}.weights.{name}"] = parameter.data

def positions_for(frames, height, width):
    shape = VideoLatentShape(batch=BATCH, channels=1, frames=frames, height=height, width=width)
    coords = VideoLatentPatchifier(patch_size=1).get_patch_grid_bounds(shape)
    positions = get_pixel_coords(coords, SpatioTemporalScaleFactors(8, 32, 32), causal_fix=True).float()
    positions[:, 0, ...] = positions[:, 0, ...] / FPS
    return positions

adaln = AdaLayerNormSingle(HIDDEN, embedding_coefficient=9)
randomize(adaln)
timesteps = torch.tensor([909.375, 0.0, 421.875])
dump_weights("adaln", adaln)
tensors["adaln.input"] = timesteps
with torch.no_grad():
    modulation, embedded = adaln(timesteps, hidden_dtype=torch.float32)
tensors["adaln.expected"] = modulation
tensors["adaln.expectedEmbedded"] = embedded

norm = torch.nn.RMSNorm(HIDDEN, eps=1e-6)
randomize(norm, scale=1.0)
norm_input = torch.randn(BATCH, CONTEXT_LENGTH, HIDDEN)
dump_weights("norm", norm)
tensors["norm.input"] = norm_input
with torch.no_grad():
    tensors["norm.expected"] = norm(norm_input)

positions = positions_for(FRAMES, HEIGHT, WIDTH)
tokens = FRAMES * HEIGHT * WIDTH
cos, sin = precompute_freqs_cis(
    positions,
    dim=HIDDEN,
    out_dtype=torch.float32,
    theta=THETA,
    max_pos=MAX_POSITIONS,
    use_middle_indices_grid=True,
    num_attention_heads=HEADS,
    rope_type=LTXRopeType.SPLIT,
    freq_grid_generator=generate_freq_grid_np,
)
tensors["rope.positions"] = positions
tensors["rope.cos"] = cos
tensors["rope.sin"] = sin
rope_input = torch.randn(BATCH, tokens, HIDDEN)
tensors["rope.input"] = rope_input
with torch.no_grad():
    tensors["rope.expected"] = apply_rotary_emb(rope_input, (cos, sin), LTXRopeType.SPLIT)

self_attention = Attention(query_dim=HIDDEN, heads=HEADS, dim_head=HEAD_DIM, apply_gated_attention=True)
randomize(self_attention)
attention_input = torch.randn(BATCH, tokens, HIDDEN)
dump_weights("selfAttention", self_attention)
tensors["selfAttention.input"] = attention_input
with torch.no_grad():
    tensors["selfAttention.expected"] = self_attention(attention_input, pe=(cos, sin))

cross_attention = Attention(query_dim=HIDDEN, context_dim=CONTEXT, heads=HEADS, dim_head=HEAD_DIM, apply_gated_attention=True)
randomize(cross_attention)
context = torch.randn(BATCH, CONTEXT_LENGTH, CONTEXT)
dump_weights("crossAttention", cross_attention)
tensors["crossAttention.input"] = attention_input
tensors["crossAttention.context"] = context
with torch.no_grad():
    tensors["crossAttention.expected"] = cross_attention(attention_input, context=context)

feed_forward = FeedForward(HIDDEN, dim_out=HIDDEN, bias=False)
randomize(feed_forward)
dump_weights("feedForward", feed_forward)
tensors["feedForward.input"] = attention_input
with torch.no_grad():
    tensors["feedForward.expected"] = feed_forward(attention_input)

block = BasicAVTransformerBlock(
    video=TransformerConfig(
        dim=HIDDEN,
        heads=HEADS,
        d_head=HEAD_DIM,
        context_dim=CONTEXT,
        apply_gated_attention=True,
        cross_attention_adaln=True,
        ff_bias=False,
    ),
    audio=None,
)
randomize(block)
block_input = torch.randn(BATCH, tokens, HIDDEN)
block_timesteps = torch.randn(BATCH, tokens, 9 * HIDDEN).mul(0.3)
block_prompt = torch.randn(BATCH, 1, 2 * HIDDEN).mul(0.3)
dump_weights("block", block)
tensors["block.input"] = block_input
tensors["block.context"] = context
tensors["block.timesteps"] = block_timesteps
tensors["block.prompt"] = block_prompt
with torch.no_grad():
    video_out, _ = block(
        TransformerArgs(
            x=block_input,
            context=context,
            context_mask=None,
            timesteps=block_timesteps,
            embedded_timestep=torch.zeros(BATCH, 1, HIDDEN),
            positional_embeddings=(cos, sin),
            cross_positional_embeddings=None,
            cross_scale_shift_timestep=None,
            cross_gate_timestep=None,
            enabled=True,
            prompt_timestep=block_prompt,
        ),
        None,
    )
tensors["block.expected"] = video_out.x

save_file(tensors, OUT)
print(f"wrote {len(tensors)} tensors to {OUT}")
