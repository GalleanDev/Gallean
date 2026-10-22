import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), "reference"))

import torch

torch.set_num_threads(1)

from vrgbx.wan_video_dit import WanModel

from st_save import save_file

OUT = sys.argv[1] if len(sys.argv) > 1 else os.path.join(
    os.path.dirname(os.path.abspath(__file__)), "wan-dit.safetensors")

LATENT_CHANNELS = 16
HEIGHT = 32
WIDTH = 32

torch.manual_seed(23)

dit = WanModel(
    dim=64,
    in_dim=32,
    ffn_dim=128,
    out_dim=16,
    text_dim=48,
    freq_dim=32,
    eps=1e-6,
    patch_size=(1, 2, 2),
    num_heads=2,
    num_layers=2,
    has_image_input=False,
    require_vae_embedding=True,
    require_clip_embedding=False)
dit.eval()

latents = torch.randn(1, LATENT_CHANNELS, 2, HEIGHT // 8, WIDTH // 8)
conditioning = torch.randn(1, LATENT_CHANNELS, 2, HEIGHT // 8, WIDTH // 8)
context = torch.randn(1, 6, 48)
timestep = torch.tensor([731.0])

with torch.no_grad():
    noise = dit(torch.cat([latents, conditioning], dim=1), timestep=timestep, context=context)

state = {"dit." + name: tensor for name, tensor in dit.state_dict().items()}
state.update({
    "latents": latents,
    "conditioning": conditioning,
    "context": context,
    "timestep": timestep,
    "noise": noise,
})

save_file(state, OUT)
print(f"noise {tuple(noise.shape)} -> {OUT}")
