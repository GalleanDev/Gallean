import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import torch

torch.set_num_threads(1)

REPO = os.environ.get("LTX2_REPO")

if REPO is None:
    raise SystemExit("Set LTX2_REPO to a checkout of Lightricks/LTX-2.")

sys.path.insert(0, os.path.join(REPO, "packages", "ltx-core", "src"))

from ltx_core.model.upsampler.model import LatentUpsampler, upsample_video
from ltx_core.model.video_vae.convolution import CausalConv3d
from ltx_core.model.video_vae.enums import LogVarianceType, NormLayerType, PaddingModeType
from ltx_core.model.video_vae.sampling import DepthToSpaceUpsample, SpaceToDepthDownsample
from ltx_core.model.video_vae.video_vae import ConvVideoDecoder, VideoEncoder

from st_save import save_file

OUT = sys.argv[1] if len(sys.argv) > 1 else os.path.join(
    os.path.dirname(os.path.abspath(__file__)), "ltx-vae.safetensors")

LATENT = 8
BASE = 4
PATCH = 4
BLOCKS = [
    ("res_x", {"num_layers": 1}),
    ("compress_space_res", {"multiplier": 2}),
    ("res_x", {"num_layers": 1}),
    ("compress_time_res", {"multiplier": 2}),
    ("res_x", {"num_layers": 1}),
    ("compress_all_res", {"multiplier": 2}),
    ("res_x", {"num_layers": 1}),
    ("compress_all_res", {"multiplier": 1}),
    ("res_x", {"num_layers": 1}),
]
DECODER_BLOCKS = [
    ("res_x", {"num_layers": 1}),
    ("compress_space", {"multiplier": 2}),
    ("res_x", {"num_layers": 1}),
    ("compress_time", {"multiplier": 2}),
    ("res_x", {"num_layers": 1}),
    ("compress_all", {"multiplier": 2}),
    ("res_x", {"num_layers": 1}),
    ("compress_all", {"multiplier": 1}),
    ("res_x", {"num_layers": 1}),
]
FRAMES = 9
SIZE = 64

torch.manual_seed(17)

tensors = {}

def randomize(module, scale=0.2):
    with torch.no_grad():
        for parameter in module.parameters():
            parameter.copy_(torch.randn_like(parameter).mul(scale))
        for name, buffer in module.named_buffers():
            if name.endswith("std-of-means"):
                buffer.copy_(torch.rand_like(buffer).add(0.5))
            elif name.endswith("mean-of-means"):
                buffer.copy_(torch.randn_like(buffer).mul(0.3))

def dump(prefix, module):
    for name, parameter in module.named_parameters():
        tensors[f"{prefix}.weights.{name}"] = parameter.data
    for name, buffer in module.named_buffers():
        tensors[f"{prefix}.weights.{name}"] = buffer.data

conv_input = torch.randn(2, 4, 5, 6, 7)
for mode, causal in (("causal", True), ("symmetric", False)):
    conv = CausalConv3d(4, 6, kernel_size=3, spatial_padding_mode=PaddingModeType.ZEROS)
    randomize(conv)
    dump(f"conv.{mode}", conv)
    tensors[f"conv.{mode}.input"] = conv_input
    with torch.no_grad():
        tensors[f"conv.{mode}.expected"] = conv(conv_input, causal=causal)

space_to_depth = SpaceToDepthDownsample(dims=3, in_channels=4, out_channels=8, stride=(2, 2, 2))
randomize(space_to_depth)
s2d_input = torch.randn(2, 4, 5, 6, 8)
dump("spaceToDepth", space_to_depth)
tensors["spaceToDepth.input"] = s2d_input
with torch.no_grad():
    tensors["spaceToDepth.expected"] = space_to_depth(s2d_input)

depth_to_space = DepthToSpaceUpsample(dims=3, in_channels=8, stride=(2, 2, 2), out_channels_reduction_factor=2)
randomize(depth_to_space)
d2s_input = torch.randn(2, 8, 3, 3, 4)
dump("depthToSpace", depth_to_space)
tensors["depthToSpace.input"] = d2s_input
with torch.no_grad():
    tensors["depthToSpace.expected"] = depth_to_space(d2s_input, causal=False)

encoder = VideoEncoder(
    convolution_dimensions=3,
    in_channels=3,
    out_channels=LATENT,
    encoder_blocks=BLOCKS,
    patch_size=PATCH,
    norm_layer=NormLayerType.PIXEL_NORM,
    latent_log_var=LogVarianceType.UNIFORM,
    encoder_spatial_padding_mode=PaddingModeType.ZEROS,
)
randomize(encoder)
video = torch.rand(1, 3, FRAMES, SIZE, SIZE).mul(2.0).sub(1.0)
dump("encoder", encoder)
tensors["encoder.input"] = video
with torch.no_grad():
    latent = encoder(video)
tensors["encoder.expected"] = latent

decoder = ConvVideoDecoder(
    convolution_dimensions=3,
    in_channels=LATENT,
    out_channels=3,
    decoder_blocks=DECODER_BLOCKS,
    patch_size=PATCH,
    norm_layer=NormLayerType.PIXEL_NORM,
    causal=False,
    timestep_conditioning=False,
    decoder_spatial_padding_mode=PaddingModeType.ZEROS,
    base_channels=BASE,
)
randomize(decoder)
with torch.no_grad():
    decoder.per_channel_statistics.load_state_dict(encoder.per_channel_statistics.state_dict())
dump("decoder", decoder)
tensors["decoder.input"] = latent
with torch.no_grad():
    tensors["decoder.expected"] = decoder(latent)

upsampler = LatentUpsampler(in_channels=LATENT, mid_channels=32, num_blocks_per_stage=1, dims=3)
randomize(upsampler)
dump("upsampler", upsampler)
tensors["upsampler.input"] = latent
with torch.no_grad():
    tensors["upsampler.expected"] = upsampler(latent)
    tensors["upsampler.expectedWrapped"] = upsample_video(latent, encoder, upsampler)

save_file(tensors, OUT)
print(f"wrote {len(tensors)} tensors to {OUT}")
