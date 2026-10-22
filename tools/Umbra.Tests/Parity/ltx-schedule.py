import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import torch

torch.set_num_threads(1)

REPO = os.environ.get("LTX2_REPO")

if REPO is None:
    raise SystemExit("Set LTX2_REPO to a checkout of Lightricks/LTX-2.")

sys.path.insert(0, os.path.join(REPO, "packages", "ltx-core", "src"))

from ltx_core.components.diffusion_steps import EulerDiffusionStep
from ltx_core.utils import to_denoised

STAGE_2_DISTILLED_SIGMAS = torch.tensor([0.909375, 0.725, 0.421875, 0.0])

def post_process_latent(denoised, denoise_mask, clean):
    return (denoised * denoise_mask + clean.float() * (1 - denoise_mask)).to(denoised.dtype)

def aggregate_lora_product(down, up, strength):
    return torch.matmul(up * strength, down).to(dtype=torch.float32)

from st_save import save_file

OUT = sys.argv[1] if len(sys.argv) > 1 else os.path.join(
    os.path.dirname(os.path.abspath(__file__)), "ltx-schedule.safetensors")

torch.manual_seed(19)

tensors = {}

sigmas = STAGE_2_DISTILLED_SIGMAS.to(torch.float32)
tensors["schedule.sigmas"] = sigmas

sample = torch.randn(1, 7, 4)
noise = torch.randn(1, 7, 4)
tensors["noiser.sample"] = sample
tensors["noiser.noise"] = noise
tensors["noiser.expected"] = torch.lerp(sample.float(), noise.float(), sigmas[0].item())

velocity = torch.randn(1, 7, 4)
timesteps = torch.zeros(1, 7)
timesteps[:, :5] = sigmas[1]
tensors["step.sample"] = sample
tensors["step.velocity"] = velocity
tensors["step.timesteps"] = timesteps
denoised = to_denoised(sample, velocity, timesteps.unsqueeze(-1))
tensors["step.denoised"] = denoised
mask = torch.zeros(1, 7, 1)
mask[:, :5] = 1.0
blended = post_process_latent(denoised, mask, sample)
tensors["step.blended"] = blended
tensors["step.expected"] = EulerDiffusionStep().step(sample, blended, sigmas, 1)

weight = torch.randn(6, 5)
down = torch.randn(2, 5)
up = torch.randn(6, 2)
tensors["lora.weight"] = weight
tensors["lora.down"] = down
tensors["lora.up"] = up
delta = aggregate_lora_product(down, up, 0.5)
tensors["lora.expected"] = delta + weight

save_file(tensors, OUT)
print(f"wrote {len(tensors)} tensors to {OUT}")
