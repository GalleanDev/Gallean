import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), "reference"))

import torch

torch.set_num_threads(1)

from vrgbx.wan_video_vae import VideoVAE_

from st_save import save_file

OUT = sys.argv[1] if len(sys.argv) > 1 else os.path.join(
    os.path.dirname(os.path.abspath(__file__)), "wan-vae.safetensors")

LATENT_CHANNELS = 16
FRAMES = 5
HEIGHT = 32
WIDTH = 32

torch.manual_seed(23)

vae = VideoVAE_(dim=8, z_dim=LATENT_CHANNELS, dim_mult=[1, 2, 4, 4], num_res_blocks=1)
vae.eval()

MEAN = torch.tensor([
    -0.7571, -0.7089, -0.9113, 0.1075, -0.1745, 0.9653, -0.1517, 1.5508,
    0.4134, -0.0715, 0.5517, -0.3632, -0.1922, -0.9497, 0.2503, -0.2921])
DEVIATION = torch.tensor([
    2.8184, 1.4541, 2.3275, 2.6558, 1.2196, 1.7708, 2.6052, 2.0743,
    3.2687, 2.1526, 2.8652, 1.5579, 1.6382, 1.1253, 2.8251, 1.9160])
SCALE = [MEAN, 1.0 / DEVIATION]

images = torch.randn(1, 3, FRAMES, HEIGHT, WIDTH)
latents = torch.randn(1, LATENT_CHANNELS, 2, HEIGHT // 8, WIDTH // 8)

with torch.no_grad():
    encoded = vae.encode(images, scale=SCALE)
    decoded = vae.decode(latents, scale=SCALE)

state = {"vae." + name: tensor for name, tensor in vae.state_dict().items()}
state.update({
    "images": images,
    "latents": latents,
    "encoded": encoded,
    "decoded": decoded,
})

save_file(state, OUT)
print(f"encoded {tuple(encoded.shape)} decoded {tuple(decoded.shape)} -> {OUT}")
