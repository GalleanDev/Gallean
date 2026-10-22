import argparse
import os

import torch
from safetensors.torch import save_file

from ltx_pipelines.utils.blocks import PromptEncoder
from ltx_pipelines.utils.model_paths import ModelPaths

PROMPT = ""

parser = argparse.ArgumentParser()
parser.add_argument("--models", required=True, help="checkpoint root with diffusion_models/, text_encoders/, vae/")
parser.add_argument("--out", required=True)
parser.add_argument("--prompt", default=PROMPT)
args = parser.parse_args()

model_paths = ModelPaths.from_split(
    transformer_path=os.path.join(args.models, "diffusion_models", "ltx-2.5-22b-distilled-transformer-bf16.safetensors"),
    text_encoder_path=os.path.join(args.models, "text_encoders", "gemma4-12b-with-proj-ltx-2.5-bf16.safetensors"),
    video_vae_path=os.path.join(args.models, "vae", "ltx-2.5-video-vae-conv-bf16.safetensors"),
    audio_vae_path=os.path.join(args.models, "vae", "ltx-2.5-audio-vae-bf16.safetensors"),
)

device = torch.device("cuda")
encoder = PromptEncoder(model_paths, torch.bfloat16, device)

with torch.inference_mode():
    (context,) = encoder([args.prompt], enhance_first_prompt=False, enhance_static_cache=False, enhance_prompt_image=None)

video = context.video_encoding.detach().to(torch.bfloat16).contiguous().cpu()
print("video_context", tuple(video.shape), video.dtype)

save_file({"video_context": video}, args.out, metadata={"prompt": args.prompt})
print("wrote", args.out, os.path.getsize(args.out), "bytes")
