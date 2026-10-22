import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), "reference"))

import torch

torch.set_num_threads(1)

from vrgbx.flow_match import FlowMatchScheduler

from st_save import save_file

OUT = sys.argv[1] if len(sys.argv) > 1 else os.path.join(
    os.path.dirname(os.path.abspath(__file__)), "wan-schedule.safetensors")

scheduler = FlowMatchScheduler(num_inference_steps=50, shift=5, sigma_min=0.0, extra_one_step=True)

save_file({"sigmas": scheduler.sigmas, "timesteps": scheduler.timesteps}, OUT)
print(f"sigmas {tuple(scheduler.sigmas.shape)} -> {OUT}")
