# Own-work color variation for the dry yard grass (a URP detail albedo, multiplied x2 over the grass, 0.5 grey is
# no change): large soft patches of greener olive, straw and bare dirt, seamless (built from periodic noise).
#   python3 draw_yard_variation.py <out.png>
import sys, numpy as np
from PIL import Image
N = 512; rng = np.random.default_rng(11)
def periodic(scale):
    f = np.fft.fftfreq(N)[:, None] ** 2 + np.fft.fftfreq(N)[None, :] ** 2
    spec = (rng.normal(size=(N, N)) + 1j * rng.normal(size=(N, N))) * np.exp(-f * (N / scale) ** 2)
    n = np.real(np.fft.ifft2(spec)); return (n - n.mean()) / (n.std() + 1e-9)
a, b, c = periodic(3.0), periodic(6.0), periodic(14.0)
green = np.clip(0.55 * a + 0.25 * c, -2, 2)          # greener olive where positive
dirt = np.clip(0.6 * b - 0.8, 0, 2)                    # bare dirt patches, sparse
img = np.zeros((N, N, 3))
base = 0.5 + 0.06 * c[..., None]                       # small brightness wobble
img[...] = base
img[..., 0] += -0.05 * green + 0.07 * dirt              # straw and dirt warmer, green cooler
img[..., 1] += 0.05 * green + 0.01 * dirt
img[..., 2] += -0.04 * green - 0.04 * dirt
img = np.clip(img, 0.3, 0.7)
Image.fromarray((img * 255).astype(np.uint8)).save(sys.argv[1]); print('ok')
