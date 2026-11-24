# Neon sign atlas for the town's night (Port Calder, invented names only). One 2048 x 2048 image on black, drawn
# additive in the game (straight alpha): twelve wide signs (1024 x 256 cells, two columns, six rows) and eight tall ones
# (256 x 512 cells along the bottom). Each is a glass tube look: a wide soft halo, a tight glow, a hot core.
# The cell order here is the order NeonBuilder.cs reads. Run: python3 tools/neon_signs.py
from PIL import Image, ImageDraw, ImageFont, ImageFilter, ImageChops
import sys

OUT = "Assets/Art/Textures/Neon/neon-atlas.png"
F = "/usr/share/fonts/truetype/google-fonts/ofl/"
ANTON = F + "anton/Anton-Regular.ttf"
ARCHIVO = F + "archivoblack/ArchivoBlack-Regular.ttf"
ITALIC = "/usr/share/fonts/truetype/google-fonts/Poppins-BoldItalic.ttf"

PINK, CYAN, AMBER, RED, GREEN, VIOLET, WHITE, BLUE = (255, 40, 150), (40, 220, 255), (255, 170, 30), (255, 40, 40), (60, 255, 110), (170, 90, 255), (255, 240, 220), (60, 110, 255)

WIDE = [  # text, font, color, frame color (or None)
    ("CALDER GRAND", ANTON, AMBER, None), ("LUCKY 9", ARCHIVO, PINK, CYAN),
    ("NIGHTOWL", ITALIC, CYAN, None), ("POOL HALL", ANTON, GREEN, AMBER),
    ("OPEN 24 HRS", ANTON, RED, None), ("COCKTAILS", ITALIC, PINK, None),
    ("ARCADE", ARCHIVO, VIOLET, CYAN), ("DINER", ANTON, RED, WHITE),
    ("THE HARBOR ROOM", ANTON, BLUE, None), ("TATTOO", ARCHIVO, RED, None),
    ("CAR WASH", ANTON, CYAN, PINK), ("EATS", ARCHIVO, AMBER, RED),
]
TALL = [("HOTEL", RED), ("BAR", PINK), ("JAZZ", BLUE), ("ROOMS", AMBER), ("CLUB", VIOLET), ("LOANS", GREEN), ("PAWN", AMBER), ("EATS", CYAN)]


def tube(mask, color, scale=1.0):
    """A lit tube from a white-on-black mask: halo + glow + hot core, RGB on black."""
    w, h = mask.size
    def tint(m, c, k): return Image.merge("RGB", [m.point(lambda v, cc=cc: int(v * cc / 255 * k)) for cc in c])
    halo = tint(mask.filter(ImageFilter.GaussianBlur(22 * scale)), color, 1.5)
    glow = tint(mask.filter(ImageFilter.GaussianBlur(7 * scale)), color, 1.25)
    body = tint(mask.filter(ImageFilter.GaussianBlur(1.2)), color, 1.0)
    core = mask.filter(ImageFilter.MinFilter(5)).filter(ImageFilter.GaussianBlur(1.5))
    hot = tint(core, tuple(int(c + (255 - c) * 0.75) for c in color), 0.9)
    out = ImageChops.add(ImageChops.add(halo, glow), body)
    return ImageChops.lighter(out, hot)


def fit(text, font_path, box_w, box_h):
    size = box_h
    while size > 8:
        f = ImageFont.truetype(font_path, size)
        l, t, r, b = f.getbbox(text)
        if r - l <= box_w and b - t <= box_h: return f, (l, t, r, b)
        size -= 2
    raise SystemExit("no fit: " + text)


def wide(text, font_path, color, frame):
    W, H = 1024, 256
    m = Image.new("L", (W, H), 0); d = ImageDraw.Draw(m)
    pad = 70 if frame else 46
    f, (l, t, r, b) = fit(text, font_path, W - 2 * pad, H - 2 * pad + 30)
    d.text(((W - (r - l)) / 2 - l, (H - (b - t)) / 2 - t), text, font=f, fill=255)
    img = tube(m, color)
    if frame:
        fm = Image.new("L", (W, H), 0)
        ImageDraw.Draw(fm).rounded_rectangle((30, 30, W - 30, H - 30), radius=42, outline=255, width=7)
        img = ImageChops.add(img, tube(fm, frame, 0.7))
    return img


def tall(text, color):
    W, H = 256, 512
    m = Image.new("L", (W, H), 0); d = ImageDraw.Draw(m)
    n = len(text); cell = (H - 80) / n
    f = None
    for ch in text:
        g, _ = fit(ch, ARCHIVO, W - 110, int(cell) - 10)
        if f is None or g.size < f.size: f = g
    for i, ch in enumerate(text):
        l, t, r, b = f.getbbox(ch)
        d.text(((W - (r - l)) / 2 - l, 40 + cell * i + (cell - (b - t)) / 2 - t), ch, font=f, fill=255)
    img = tube(m, color)
    fm = Image.new("L", (W, H), 0)
    ImageDraw.Draw(fm).rounded_rectangle((22, 14, W - 22, H - 14), radius=30, outline=255, width=6)
    return ImageChops.add(img, tube(fm, WHITE if color != WHITE else CYAN, 0.6))


atlas = Image.new("RGB", (2048, 2048), (0, 0, 0))
for i, s in enumerate(WIDE): atlas.paste(wide(*s), ((i % 2) * 1024, (i // 2) * 256))
for i, s in enumerate(TALL): atlas.paste(tall(*s), (i * 256, 1536))
# straight alpha: alpha is the brightest channel and the color is divided by it, so color x alpha is the picture
# above and the black of a cell adds nothing even where fog tints it
r, g, b = atlas.split()
a = ImageChops.lighter(ImageChops.lighter(r, g), b)
def unmul(c): return Image.frombytes("L", c.size, bytes(min(255, (cv * 255 + av // 2) // av) if av else 0 for cv, av in zip(c.tobytes(), a.tobytes())))
atlas = Image.merge("RGBA", (unmul(r), unmul(g), unmul(b), a))
atlas.save(OUT, optimize=True)
print(OUT, atlas.size)
