from pathlib import Path

import numpy as np
from PIL import Image


def fill_neon(src_path: Path, dst_path: Path) -> None:
    im = Image.open(src_path).convert("RGBA")
    arr = np.array(im)
    rgb = arr[:, :, :3].astype(np.float32)
    a = arr[:, :, 3].astype(np.float32)
    lum = rgb.max(axis=2)
    sat = (rgb.max(axis=2) - rgb.min(axis=2)) / (rgb.max(axis=2) + 1e-5)

    walkable = ((lum < 28) & (a > 0)) | (a < 8)
    h, w = walkable.shape
    exterior = np.zeros((h, w), dtype=bool)
    stack = []
    for x in range(w):
        stack.append((0, x))
        stack.append((h - 1, x))
    for y in range(h):
        stack.append((y, 0))
        stack.append((y, w - 1))

    while stack:
        y, x = stack.pop()
        if y < 0 or y >= h or x < 0 or x >= w:
            continue
        if exterior[y, x] or not walkable[y, x]:
            continue
        exterior[y, x] = True
        stack.extend(((y - 1, x), (y + 1, x), (y, x - 1), (y, x + 1)))

    inside = ~exterior
    # Keep bright neon rim; fill the rest of the inside (closes black moat)
    rim = inside & ((lum > 150) | ((sat > 0.35) & (lum > 100)))
    fill_zone = inside & (~rim)
    if not fill_zone.any():
        print(f"NO FILL ZONE: {src_path.name}")
        im.save(dst_path)
        return

    neon_mask = rim & (sat > 0.2)
    if neon_mask.sum() < 50:
        neon_mask = rim
    mean_rgb = rgb[neon_mask].mean(axis=0)
    fill_rgb = np.clip(mean_rgb * 0.88 + 28.0, 0, 255)

    ys, xs = np.where(fill_zone)
    cy, cx = float(ys.mean()), float(xs.mean())
    yy, xx = np.ogrid[:h, :w]
    dist = np.sqrt((yy - cy) ** 2 + (xx - cx) ** 2)
    maxd = float(dist[fill_zone].max()) + 1e-5
    falloff = 1.0 - 0.12 * (dist / maxd)

    out = arr.copy().astype(np.float32)
    for c in range(3):
        out[:, :, c][fill_zone] = fill_rgb[c] * falloff[fill_zone]
    out[:, :, 3][fill_zone] = 255

    Image.fromarray(np.clip(out, 0, 255).astype(np.uint8), "RGBA").save(dst_path)
    print(
        f"OK {src_path.name} -> {dst_path.name} "
        f"fill={int(fill_zone.sum())} color={fill_rgb.round().astype(int)}"
    )


def main() -> None:
    src_dir = Path(r"c:\GitHub\RandomPlanetDefense\Assets\Images\Enemy")
    out_dir = src_dir / "Filled"
    out_dir.mkdir(exist_ok=True)
    names = [
        "BlueCircle.png",
        "BlueSquare.png",
        "GreenCircle.png",
        "GreenSquare.png",
        "RedCircle.png",
        "RedSquare.png",
        "PinkStar.png",
    ]
    for name in names:
        p = src_dir / name
        if p.exists():
            fill_neon(p, out_dir / name.replace(".png", "_Filled.png"))


if __name__ == "__main__":
    main()
