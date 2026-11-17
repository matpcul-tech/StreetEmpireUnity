# Own-work decal atlas for the Eastside (cracks, oil stains, litter, graffiti) and the dry plant blades,
# drawn with PIL. Graffiti lettering uses the Bangers font (SIL Open Font License).
import math, random
from PIL import Image, ImageDraw, ImageFilter, ImageFont
random.seed(7)
A = Image.new('RGBA', (1024, 1024), (0, 0, 0, 0))
def tile(ix, iy, w=256, h=256): return Image.new('RGBA', (w, h), (0, 0, 0, 0))
def crack(seed, w=256):
    random.seed(seed); im = tile(0, 0); g = ImageDraw.Draw(im)
    def branch(x, y, a, L, wd, depth):
        for _ in range(int(L)):
            a += random.uniform(-0.5, 0.5); nx, ny = x + math.cos(a) * 6, y + math.sin(a) * 6
            g.line([(x, y), (nx, ny)], fill=(24, 20, 18, 230), width=max(1, int(wd))); x, y = nx, ny
            wd *= 0.97
            if depth < 3 and random.random() < 0.08: branch(x, y, a + random.choice([-1, 1]) * random.uniform(0.6, 1.2), L * 0.5, wd * 0.8, depth + 1)
            if not (6 < x < w - 6 and 6 < y < w - 6): break
    branch(20, w / 2 + random.uniform(-30, 30), random.uniform(-0.3, 0.3), 40, 4, 0)
    branch(w / 2, w / 2, random.uniform(1.2, 2.0), 22, 3, 1)
    return im
def oil(seed):
    random.seed(seed); im = Image.new('L', (256, 256), 0); g = ImageDraw.Draw(im)
    for _ in range(14):
        r = random.uniform(18, 60); x = 128 + random.gauss(0, 30); y = 128 + random.gauss(0, 22)
        g.ellipse([x - r, y - r * 0.7, x + r, y + r * 0.7], fill=random.randint(60, 140))
    im = im.filter(ImageFilter.GaussianBlur(14))
    for _ in range(30):
        x, y, r = random.uniform(40, 216), random.uniform(50, 206), random.uniform(2, 7)
        g = ImageDraw.Draw(im); g.ellipse([x - r, y - r, x + r, y + r], fill=170)
    im = im.filter(ImageFilter.GaussianBlur(2))
    out = Image.new('RGBA', (256, 256), (14, 12, 10, 0)); out.putalpha(im.point(lambda v: min(200, int(v * 1.3)))); return out
def litter(seed):
    random.seed(seed); im = tile(0, 0); g = ImageDraw.Draw(im)
    for _ in range(9):   # paper and wrappers
        x, y = random.uniform(30, 226), random.uniform(30, 226); w, h = random.uniform(10, 26), random.uniform(7, 18); a = random.uniform(0, 3.14)
        pts = [(x + math.cos(a) * dx - math.sin(a) * dy, y + math.sin(a) * dx + math.cos(a) * dy) for dx, dy in [(-w, -h), (w, -h), (w, h), (-w, h)]]
        g.polygon(pts, fill=random.choice([(214, 204, 180, 255), (190, 180, 160, 255), (160, 60, 50, 255), (220, 190, 80, 255), (90, 120, 160, 255)]))
    for _ in range(4):   # cans and cups
        x, y = random.uniform(30, 226), random.uniform(30, 226)
        g.ellipse([x - 9, y - 5, x + 9, y + 5], fill=random.choice([(170, 40, 36, 255), (180, 182, 186, 255), (230, 228, 220, 255)]))
    for _ in range(14):  # butts and bits
        x, y = random.uniform(20, 236), random.uniform(20, 236); g.line([(x, y), (x + random.uniform(-6, 6), y + random.uniform(-3, 3))], fill=(220, 200, 160, 255), width=2)
    dirt = Image.new('L', (256, 256), 0); dg = ImageDraw.Draw(dirt)
    for _ in range(8): x, y, r = random.uniform(40, 216), random.uniform(40, 216), random.uniform(20, 50); dg.ellipse([x - r, y - r, x + r, y + r], fill=70)
    dirt = dirt.filter(ImageFilter.GaussianBlur(16)); base = Image.new('RGBA', (256, 256), (40, 32, 24, 0)); base.putalpha(dirt)
    return Image.alpha_composite(base, im)
FONT = '/usr/share/fonts/truetype/sand-box/google/Bangers/Bangers-Regular.ttf'
def graffiti(word, fill, line, seed):
    random.seed(seed); im = Image.new('RGBA', (512, 256), (0, 0, 0, 0)); g = ImageDraw.Draw(im)
    f = ImageFont.truetype(FONT, 150); bb = g.textbbox((0, 0), word, font=f); tw, th = bb[2] - bb[0], bb[3] - bb[1]
    s = min(1.0, 470 / tw); f = ImageFont.truetype(FONT, int(150 * s)); bb = g.textbbox((0, 0), word, font=f); tw, th = bb[2] - bb[0], bb[3] - bb[1]
    x, y = (512 - tw) / 2 - bb[0], (256 - th) / 2 - bb[1] - 6
    g.text((x + 6, y + 6), word, font=f, fill=(10, 8, 8, 220), stroke_width=10, stroke_fill=(10, 8, 8, 220))
    g.text((x, y), word, font=f, fill=fill, stroke_width=7, stroke_fill=line)
    for _ in range(10):  # drips
        dx = random.uniform(x + 10, x + tw - 10); dy = y + th * random.uniform(0.75, 1.05) + bb[1]
        g.line([(dx, dy), (dx, dy + random.uniform(10, 40))], fill=fill, width=4)
    im = im.rotate(random.uniform(-4, 4), resample=Image.BICUBIC)
    a = im.split()[3].point(lambda v: int(v * 0.92)); im.putalpha(a); return im
tiles = [crack(1), crack(2), oil(3), oil(4), litter(5), litter(6), crack(8), oil(9)]
for i, t in enumerate(tiles): A.paste(t, ((i % 4) * 256, (i // 4) * 256))
G = [graffiti('EASTSIDE', (236, 80, 160, 255), (250, 246, 236, 255), 11), graffiti('LOCALS', (60, 200, 210, 255), (24, 20, 40, 255), 12),
     graffiti('RED K', (220, 44, 40, 255), (250, 230, 120, 255), 13), graffiti('EMPIRE', (150, 90, 220, 255), (250, 250, 250, 255), 14)]
for i, t in enumerate(G): A.paste(t, ((i % 2) * 512, 512 + (i // 2) * 256))
A.save('tex/decal-atlas.png')
# dry plant blades for the shrubs and the yard grass rows: olive and straw, like the web game's dry grass
B = Image.new('RGBA', (256, 256), (0, 0, 0, 0)); g = ImageDraw.Draw(B); random.seed(21)
for _ in range(150):
    x = random.uniform(6, 250); h = random.uniform(120, 250); lean = random.uniform(-40, 40)
    col = random.choice([(0x56, 0x56, 0x2f), (0x6a, 0x6a, 0x36), (0x8a, 0x7a, 0x4a), (0x46, 0x53, 0x2a), (0xa0, 0x8c, 0x58)])
    g.line([(x, 256), (x + lean * 0.5, 256 - h * 0.5), (x + lean, 256 - h)], fill=col + (255,), width=random.choice([2, 3, 3, 4]))
B.save('tex/dry-blades.png')
print('ok')
