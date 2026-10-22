import argparse
import json
import os
import struct
from pathlib import Path

import torch
from ltx_pipelines.utils.blocks import PromptEncoder
from ltx_pipelines.utils.model_paths import ModelPaths

DTYPE = "BF16"
SHAPE = [1, 1024, 4096]
PAYLOAD = 1024 * 4096 * 2
ALIGNMENT = 8

parser = argparse.ArgumentParser()
parser.add_argument("--models", required=True, help="LTX-2.5 root with diffusion_models/, text_encoders/, vae/")
parser.add_argument("--prompts", required=True, help="directory with prompt files")
parser.add_argument("--out", required=True, help="path to .safetensors output file")
parser.add_argument("--device", default="cuda")
parser.add_argument("--batch", type=int, default=16, help="prompts per encoder load")
args = parser.parse_args()

root = Path(args.prompts)
prompts = []
for path in sorted(root.rglob("*.txt")):
    name = path.relative_to(root).with_suffix("").as_posix()
    prompts.append((name, path.read_text(encoding="utf-8").strip()))

print(f"{len(prompts)} prompts", flush=True)

model_paths = ModelPaths.from_split(
    transformer_path=os.path.join(args.models, "diffusion_models", "ltx-2.5-22b-distilled-transformer-bf16.safetensors"),
    text_encoder_path=os.path.join(args.models, "text_encoders", "gemma4-12b-with-proj-ltx-2.5-bf16.safetensors"),
    video_vae_path=os.path.join(args.models, "vae", "ltx-2.5-video-vae-conv-bf16.safetensors"),
    audio_vae_path=os.path.join(args.models, "vae", "ltx-2.5-audio-vae-bf16.safetensors"),
)

encoder = PromptEncoder(model_paths, torch.bfloat16, torch.device(args.device))

header = {}
offset = 0
for name, _ in prompts:
    header[name] = {"dtype": DTYPE, "shape": SHAPE, "data_offsets": [offset, offset + PAYLOAD]}
    offset += PAYLOAD

blob = json.dumps(header, separators=(",", ":")).encode("utf-8")
blob += b" " * ((ALIGNMENT - (len(blob) % ALIGNMENT)) % ALIGNMENT)

out = Path(args.out)
out.parent.mkdir(parents=True, exist_ok=True)
written = 0

with open(out, "wb") as handle:
    handle.write(struct.pack("<Q", len(blob)))
    handle.write(blob)

    for start in range(0, len(prompts), args.batch):
        chunk = prompts[start:start + args.batch]

        with torch.inference_mode():
            contexts = encoder(
                [text for _, text in chunk],
                enhance_first_prompt=False,
                enhance_static_cache=False,
                enhance_prompt_image=None,
            )

        for (name, _), context in zip(chunk, contexts):
            video = context.video_encoding.detach().to(torch.bfloat16).contiguous().cpu()
            payload = video.view(torch.int16).numpy().tobytes()

            if len(payload) != PAYLOAD:
                raise SystemExit(f"{name} encoded to {len(payload)} bytes, want {PAYLOAD}.")

            handle.write(payload)
            written += 1
            print(f"[{written}/{len(prompts)}] {name}", flush=True)

print(f"wrote {out} ({out.stat().st_size} bytes)", flush=True)
