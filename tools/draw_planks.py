# Own-work fence board atlas: 8 weathered vertical cedar boards side by side (128 x 1024 each), grain along V,
# a few knots, end checks and grey weathering, so one board maps onto one plank at real scale.
import numpy as np
from PIL import Image, ImageFilter
rng = np.random.default_rng(5); W, H, B = 1024, 1024, 8
img = np.zeros((H, W, 3))
y = np.arange(H)[:, None]; x = np.arange(W // B)[None, :]
for b in range(B):
    base = np.array([0.50, 0.37, 0.25]) * rng.uniform(0.82, 1.1) + rng.uniform(-0.03, 0.03, 3)
    grey = rng.uniform(0.15, 0.45)   # sun greyed
    base = base * (1 - grey) + np.array([0.46, 0.43, 0.39]) * grey
    # grain: thin wavy lines along V
    ph = rng.uniform(0, 6.28, 6); fr = rng.uniform(0.15, 0.5, 6)
    wav = sum(np.sin(x * f + 3.0 * np.sin(y / rng.uniform(90, 260) + p) + p) for f, p in zip(fr, ph)) / 6
    streak = np.repeat(rng.normal(0, 1, (1, W // B)), H, 0)
    streak = np.array(Image.fromarray(((streak + 3) * 40).astype(np.uint8)).filter(ImageFilter.GaussianBlur(1.2))) / 40 - 3
    v = 1 + 0.07 * wav + 0.05 * streak + 0.03 * rng.normal(0, 1, (H, W // B))
    tile = base[None, None, :] * v[..., None]
    for _ in range(rng.integers(1, 4)):   # knots
        kx, ky, kr = rng.uniform(25, 103), rng.uniform(60, 960), rng.uniform(5, 11)
        d = np.sqrt((x - kx) ** 2 + ((y - ky) / 1.6) ** 2)
        tile *= (1 - 0.45 * np.exp(-(d / kr) ** 2))[..., None]
        tile *= (1 - 0.12 * np.exp(-((d - kr * 1.6) / 3) ** 2))[..., None]
    edge = np.minimum(x, W // B - 1 - x)   # darker board edges and dirty bottom
    tile *= (0.78 + 0.22 * np.clip(edge / 6, 0, 1))[..., None]
    tile *= (0.8 + 0.2 * np.clip((H - 1 - y) / 150, 0, 1))[..., None]   # image bottom is the ground end
    img[:, b * (W // B):(b + 1) * (W // B)] = tile
Image.fromarray((np.clip(img, 0, 1) * 255).astype(np.uint8)).save('tex/fence-boards.png'); print('ok')
