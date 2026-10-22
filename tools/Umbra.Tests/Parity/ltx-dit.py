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
from ltx_core.model.transformer.modality import Modality
from ltx_core.model.transformer.model import LTXModel, LTXModelType
from ltx_core.types import SpatioTemporalScaleFactors, VideoLatentShape

from st_save import save_file

OUT = sys.argv[1] if len(sys.argv) > 1 else os.path.join(
    os.path.dirname(os.path.abspath(__file__)), "ltx-dit.safetensors")

HIDDEN = 16
HEADS = 2
HEAD_DIM = 8
CHANNELS = 6
CONTEXT = 16
CONTEXT_LENGTH = 5
DEPTH = 2
BATCH = 2
FPS = 12.0
SIGMA = 0.909375

torch.manual_seed(13)

def positions_for(frames, height, width, scale):
    shape = VideoLatentShape(batch=BATCH, channels=CHANNELS, frames=frames, height=height, width=width)
    coords = VideoLatentPatchifier(patch_size=1).get_patch_grid_bounds(shape)
    positions = get_pixel_coords(coords, SpatioTemporalScaleFactors(8, 32, 32), causal_fix=True).float()
    positions[:, 0, ...] = positions[:, 0, ...] / FPS
    positions[:, 1, ...] *= scale
    positions[:, 2, ...] *= scale
    return positions

model = LTXModel(
    model_type=LTXModelType.VideoOnly,
    num_attention_heads=HEADS,
    attention_head_dim=HEAD_DIM,
    in_channels=CHANNELS,
    out_channels=CHANNELS,
    num_layers=DEPTH,
    cross_attention_dim=CONTEXT,
    positional_embedding_theta=10000.0,
    positional_embedding_max_pos=[20, 2048, 2048],
    timestep_scale_multiplier=1000,
    double_precision_rope=True,
    apply_gated_attention=True,
    cross_attention_adaln=True,
    use_prompt_adaln_single=True,
    ff_bias=False,
    use_keyframes_abs_pos_embedding=True,
)
with torch.no_grad():
    for parameter in model.parameters():
        parameter.copy_(torch.randn_like(parameter).mul(0.2))

target_tokens = 2 * 2 * 3
reference_tokens = 2 * 1 * 2
tokens = target_tokens + reference_tokens
positions = torch.cat([positions_for(2, 2, 3, 1), positions_for(2, 1, 2, 2)], dim=2)
latent = torch.randn(BATCH, tokens, CHANNELS)
context = torch.randn(BATCH, CONTEXT_LENGTH, CONTEXT)
sigma = torch.full((BATCH,), SIGMA)
timesteps = torch.zeros(BATCH, tokens)
timesteps[:, :target_tokens] = SIGMA
keyframes = torch.zeros(BATCH, tokens, 1)
keyframes[:, : 2 * 3] = 1.0

tensors = {}
for name, parameter in model.named_parameters():
    tensors[f"dit.weights.{name}"] = parameter.data
tensors["dit.latent"] = latent
tensors["dit.context"] = context
tensors["dit.sigma"] = sigma
tensors["dit.timesteps"] = timesteps
tensors["dit.positions"] = positions
tensors["dit.keyframes"] = keyframes

with torch.no_grad():
    velocity, _ = model(
        video=Modality(
            latent=latent,
            sigma=sigma,
            timesteps=timesteps,
            positions=positions,
            context=context,
            keyframes_mask=keyframes,
        ),
        audio=None,
        perturbations=None,
    )
tensors["dit.expected"] = velocity

save_file(tensors, OUT)
print(f"wrote {len(tensors)} tensors to {OUT}")
