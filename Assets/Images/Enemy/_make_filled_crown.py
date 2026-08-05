from pathlib import Path

import numpy as np
from PIL import Image


def main() -> None:
    """KingCrown: B channel is often 255, so use alpha (not max-lum) for masks."""
    src = Path(r"c:\GitHub\RandomPlanetDefense\Assets\Images\Enemy\KingCrown.png")
    dst = Path(r"c:\GitHub\RandomPlanetDefense\Assets\Images\Enemy\Filled\KingCrown_Filled.png")

    arr = np.array(Image.open(src).convert("RGBA"))
    rgb = arr[:, :, :3].astype(np.float32)
    a = arr[:, :, 3].astype(np.float32)
    sat = (rgb.max(axis=2) - rgb.min(axis=2)) / (rgb.max(axis=2) + 1e-5)

    ink = a > 35
    h, w = ink.shape
    span = np.zeros_like(ink, dtype=bool)
    for x in range(w):
        ys = np.flatnonzero(ink[:, x])
        if ys.size < 2:
            continue
        y0, y1 = int(ys[0]), int(ys[-1])
        if y1 - y0 < 10:
            continue
        span[y0 : y1 + 1, x] = True

    core = a > 190
    fill_zone = span & (~core)

    neon = (a > 80) & (sat > 0.15)
    mean = rgb[neon].mean(axis=0) if neon.any() else np.array([180, 80, 255], np.float32)
    fill_rgb = np.clip(mean * 0.75 + 50.0, 0, 255)

    ys, xs = np.where(fill_zone)
    print(f"span={int(span.sum())} fill={len(ys)} core={int(core.sum())} color={fill_rgb.round().astype(int)}")

    cy, cx = float(ys.mean()), float(xs.mean())
    dist = np.sqrt((ys.astype(np.float32) - cy) ** 2 + (xs.astype(np.float32) - cx) ** 2)
    falloff = 1.0 - 0.08 * (dist / (float(dist.max()) + 1e-5))

    out = arr.copy().astype(np.float32)
    for c in range(3):
        out[ys, xs, c] = fill_rgb[c] * falloff
    out[ys, xs, 3] = 255

    cy2, cx2 = np.where(core)
    out[cy2, cx2] = arr[cy2, cx2]

    Image.fromarray(np.clip(out, 0, 255).astype(np.uint8), "RGBA").save(dst)
    print(f"OK -> {dst}")


if __name__ == "__main__":
    main()
