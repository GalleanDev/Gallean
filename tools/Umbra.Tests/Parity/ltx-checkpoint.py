import argparse
import os
import sys
from dataclasses import replace

import numpy as np
import torch
from PIL import Image
from safetensors.torch import load_file, save_file

from ltx_core.loader import LTXV_LORA_COMFY_RENAMING_MAP, LoraPathStrengthAndSDOps
from ltx_core.model.video_vae import AUTO_TILING
from ltx_core.types import VideoPixelShape
import ltx_pipelines.iclora_utils as iclora_utils
from ltx_pipelines.ic_lora import ICLoraPipeline
from ltx_pipelines.utils.denoisers import SimpleDenoiser
from ltx_pipelines.utils.helpers import ensure_tiling_config, tiling_scale_factors_for_vae
from ltx_pipelines.utils.model_paths import ModelPaths
from ltx_pipelines.utils.types import ModalitySpec, OffloadMode

parser = argparse.ArgumentParser()
parser.add_argument("--models", required=True)
parser.add_argument("--lora", required=True)
parser.add_argument("--context", required=True)
parser.add_argument("--dumps", required=True)
parser.add_argument("--out", required=True)
parser.add_argument("--offload", default="cpu", choices=["cpu", "none"])
parser.add_argument("--fps", type=float, default=12.0)
parser.add_argument("--lora-strength", type=float, default=0.5)
args = parser.parse_args()

os.makedirs(args.out, exist_ok=True)
device = torch.device("cuda")
SIGMAS = torch.tensor([0.909375, 0.725, 0.421875, 0.0], dtype=torch.float32, device=device)

def load(name):
    return next(iter(load_file(os.path.join(args.dumps, f"{name}.safetensors")).values()))

def report(label, mine, theirs):
    mine = mine.detach().float().cpu()
    theirs = theirs.detach().float().cpu()
    if mine.shape != theirs.shape:
        print(f"{label}: SHAPE port {tuple(theirs.shape)} vs reference {tuple(mine.shape)}", flush=True)
        return
    d = (mine - theirs).abs()
    print(f"{label}: max {d.max():.4f} mean {d.mean():.5f} (reference std {mine.std():.4f}, port std {theirs.std():.4f})", flush=True)

video = load("video")
noise = load("noise")
_, _, F, H, W = video.shape
print(f"video {tuple(video.shape)} noise {tuple(noise.shape)}", flush=True)

frames_u8 = ((video[0].permute(1, 2, 3, 0) + 1.0) * 127.5).round().clamp(0, 255).to(torch.uint8)

def frames_gen(path, device, starting_frame=0, frame_cap=None):
    for index in range(starting_frame, F if frame_cap is None else min(F, starting_frame + frame_cap)):
        yield frames_u8[index].to(device).unsqueeze(0)

iclora_utils.decode_video_by_frame = frames_gen

model_paths = ModelPaths.from_split(
    transformer_path=os.path.join(args.models, "diffusion_models", "ltx-2.5-22b-distilled-transformer-bf16.safetensors"),
    text_encoder_path=os.path.join(args.models, "text_encoders", "gemma4-12b-with-proj-ltx-2.5-bf16.safetensors"),
    video_vae_path=os.path.join(args.models, "vae", "ltx-2.5-video-vae-conv-bf16.safetensors"),
    audio_vae_path=os.path.join(args.models, "vae", "ltx-2.5-audio-vae-bf16.safetensors"),
)
lora = LoraPathStrengthAndSDOps(args.lora, args.lora_strength, LTXV_LORA_COMFY_RENAMING_MAP)
pipe = ICLoraPipeline(
    model_paths=model_paths,
    spatial_upsampler_path=os.path.join(args.models, "latent_upscale_models", "ltx-2.5-latent-spatial-upscaler-x2-bf16-1.0.safetensors"),
    loras=[lora],
    device=device,
    offload_mode=OffloadMode.CPU if args.offload == "cpu" else OffloadMode.NONE,
)

video_context = load_file(args.context)["video_context"].to(device=device, dtype=torch.bfloat16)
print(f"context {tuple(video_context.shape)}", flush=True)

width, height = 2 * W, 2 * H

with torch.inference_mode():
    conds = pipe.image_conditioner(
        lambda enc: pipe._create_conditionings(
            images=[],
            video_conditioning=[("dumped", 1.0)],
            height=height,
            width=width,
            video_encoder=enc,
            num_frames=F,
            conditioning_attention_strength=1.0,
            conditioning_attention_mask=None,
            color_space=None,
        )
    )
    reference = conds[-1].latent
    report("reference latent", reference, load("reference"))

    initial = pipe.upsampler(reference)
    report("upsampled initial", initial, load("initial"))

    target_tokens = initial.shape[2] * initial.shape[3] * initial.shape[4]

    class FileNoiser:
        def __call__(self, state, noise_scale=1.0):
            drawn = noise.to(device=state.latent.device, dtype=state.latent.dtype)
            assert drawn.shape == state.latent.shape, (drawn.shape, state.latent.shape)
            latent = torch.lerp(state.latent.float(), drawn.float(), noise_scale)
            latent = torch.lerp(state.clean_latent.float(), latent, state.denoise_mask)
            return replace(state, latent=latent.to(state.latent.dtype))

    class RecordingDenoiser(SimpleDenoiser):
        def __init__(self, v, a):
            super().__init__(v, a)
            self.inputs = []
            self.outputs = []

        def __call__(self, transformer, video_state, audio_state, sigmas, step_index):
            self.inputs.append(video_state.latent.detach().float().cpu().clone())
            v, a = super().__call__(transformer, video_state, audio_state, sigmas, step_index)
            self.outputs.append(v.denoised.detach().float().cpu().clone())
            return v, a

    denoiser = RecordingDenoiser(video_context, None)
    detailing = pipe.stage_1.with_loras((lora,))
    state, _ = detailing(
        denoiser=denoiser,
        sigmas=SIGMAS,
        noiser=FileNoiser(),
        width=width,
        height=height,
        frames=F,
        fps=args.fps,
        video=ModalitySpec(context=video_context, conditionings=conds, noise_scale=SIGMAS[0].item(), initial_latent=initial),
        audio=None,
    )

    report("latents-init", denoiser.inputs[0][:, :target_tokens], load("latents-init"))
    for step in range(3):
        previous = load("latents-init") if step == 0 else load(f"latents-{step - 1}")
        port_x0 = previous - load(f"velocity-{step}") * SIGMAS[step].item()
        report(f"x0 step {step}", denoiser.outputs[step][:, :target_tokens], port_x0)
        if step < 2:
            report(f"latents-{step}", denoiser.inputs[step + 1][:, :target_tokens], load(f"latents-{step}"))

    final = state.latent
    report("denoised", final, load("denoised"))
    save_file({"denoised": final.float().cpu().contiguous()}, os.path.join(args.out, "reference-denoised.safetensors"))

    scale_factors = tiling_scale_factors_for_vae(pipe.video_decoder.checkpoint_path)
    tiling = ensure_tiling_config(
        AUTO_TILING,
        scale_factors=scale_factors,
        vae_checkpoint_path=pipe.video_decoder.checkpoint_path,
        video_shape=VideoPixelShape(batch=1, frames=F, height=height, width=width, fps=args.fps),
        diffvae_optimization=pipe.video_decoder.diffvae_optimization,
        device=device,
    )

    def save(latent, folder):
        os.makedirs(folder, exist_ok=True)
        written = 0
        for chunk in pipe.video_decoder(latent.to(device=device, dtype=torch.bfloat16), tiling, torch.Generator(device=device).manual_seed(1), dtype=torch.bfloat16):
            pixels = (chunk.detach().float().clamp(0.0, 1.0) * 255.0).round().to(torch.uint8).cpu().numpy()
            for frame in pixels:
                Image.fromarray(frame).save(os.path.join(folder, "%04d.png" % written))
                written += 1
        return written

    print("decoded reference frames:", save(final, os.path.join(args.out, "reference")), flush=True)
    print("decoded port latents through the reference decoder:", save(load("denoised"), os.path.join(args.out, "port-latents")), flush=True)
print("PARITYDONE", flush=True)
