// Paint colors for the v2 cars: reads <textures dir>/<kind>_body.jpg (written by car_v2.mjs) and writes one
// recolored copy per color, <kind>_body_<color>.jpg. The body maps come with no paint mask, so the paint is found
// by color: a texel is paint when it points the same way as the car's paint color (so shaded and lit paint both
// count) and is not far off its brightness (so black trim, tires, chrome and lights are left alone). Painted texels
// take the new color at the brightness they had, which keeps the baked shading.
// The van (white, with white lights and lettering that would take the color too), the taxi and the cruiser keep
// their one paint. Run from a folder with sharp. Usage: node car_paint.mjs <textures dir>
import sharp from 'sharp';
const dir = process.argv[2];
const COLORS = {
  black: [22, 22, 24], white: [225, 225, 220], silver: [150, 155, 160], maroon: [96, 22, 34], navy: [30, 44, 92], tan: [176, 150, 104],
  green: [28, 72, 50], red: [150, 28, 28], purple: [64, 34, 96], teal: [30, 90, 94], gold: [176, 138, 46], blue: [36, 70, 140],
};
// paint: the car's own paint color in its map; cos and lum: how close a texel must be in direction and brightness
export const CARS = {
  'sedan-clean': { paint: [34, 64, 72], cos: 0.975, lum: [0.35, 1.8], colors: ['black', 'silver', 'maroon', 'white'] },
  sedan: { paint: [116, 138, 138], cos: 0.992, lum: [0.7, 1.45], colors: ['navy', 'tan', 'green', 'red'] },
  coupe: { paint: [56, 56, 82], cos: 0.988, lum: [0.72, 1.6], colors: ['red', 'silver', 'white', 'green'] },
  lowrider: { paint: [104, 16, 40], cos: 0.95, lum: [0.3, 2.0], colors: ['purple', 'teal', 'black', 'gold'] },
  pickup: { paint: [164, 72, 72], cos: 0.975, lum: [0.35, 1.6], colors: ['blue', 'white', 'green', 'tan'] },
  suv: { paint: [24, 56, 40], cos: 0.972, lum: [0.35, 2.2], colors: ['black', 'navy', 'maroon', 'silver'] },
};
const L = (r, g, b) => 0.3 * r + 0.6 * g + 0.1 * b;
for (const [kind, c] of Object.entries(CARS)) {
  const { data, info } = await sharp(`${dir}/${kind}_body.jpg`).removeAlpha().raw().toBuffer({ resolveWithObject: true });
  const [pr, pg, pb] = c.paint, pn = Math.hypot(pr, pg, pb), pl = L(pr, pg, pb);
  const w = new Float32Array(data.length / 3); let area = 0;
  for (let i = 0, j = 0; i < data.length; i += 3, j++) {
    const r = data[i], g = data[i + 1], b = data[i + 2], n = Math.hypot(r, g, b) || 1;
    const cos = (r * pr + g * pg + b * pb) / (n * pn), k = L(r, g, b) / pl;
    if (k < c.lum[0] || k > c.lum[1]) continue;
    const t = Math.min(1, Math.max(0, (cos - (c.cos - 0.012)) / 0.012)); w[j] = t * t * (3 - 2 * t); area += w[j];
  }
  for (const name of c.colors) {
    const [tr, tg, tb] = COLORS[name], out = Buffer.from(data);
    for (let i = 0, j = 0; i < data.length; i += 3, j++) {
      if (w[j] === 0) continue;
      const k = Math.min(1.7, L(data[i], data[i + 1], data[i + 2]) / pl), a = w[j];
      out[i] = Math.min(255, data[i] * (1 - a) + tr * k * a); out[i + 1] = Math.min(255, data[i + 1] * (1 - a) + tg * k * a); out[i + 2] = Math.min(255, data[i + 2] * (1 - a) + tb * k * a);
    }
    await sharp(out, { raw: { width: info.width, height: info.height, channels: 3 } }).jpeg({ quality: 88 }).toFile(`${dir}/${kind}_body_${name}.jpg`);
  }
  console.log(kind.padEnd(12), 'paint is', (100 * area / w.length).toFixed(0) + '% of the map,', c.colors.join(' '));
}
