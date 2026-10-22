import json
import struct

import torch

DTYPES = {
    torch.float32: "F32",
    torch.float64: "F64",
    torch.float16: "F16",
    torch.int64: "I64",
    torch.int32: "I32",
    torch.uint8: "U8",
    torch.bool: "BOOL",
}


def save_file(tensors, path):
    header = {}
    blobs = []
    offset = 0

    for name, tensor in tensors.items():
        tensor = tensor.detach().cpu().contiguous()
        blob = tensor.numpy().tobytes()

        header[name] = {
            "dtype": DTYPES[tensor.dtype],
            "shape": list(tensor.shape),
            "data_offsets": [offset, offset + len(blob)],
        }

        blobs.append(blob)
        offset += len(blob)

    encoded = json.dumps(header, separators=(",", ":")).encode("utf-8")
    padding = (-len(encoded)) % 8
    encoded = encoded + b" " * padding

    with open(path, "wb") as handle:
        handle.write(struct.pack("<Q", len(encoded)))
        handle.write(encoded)

        for blob in blobs:
            handle.write(blob)

    return path
