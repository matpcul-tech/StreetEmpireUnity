# u75p: lettered sign_lit boards on the stores already in the game

Hung as separate Unity props (liquor-board method). No Meshy credits. The six v5 Meshy models stay out of
the game (they are being remeshed separately). No tire shop day swap, no new houses, no decimation of any
finished file, no washes, tints, extra window quads, or LODs.

The liquor store (1,572 triangles) and the auto garage (38,242 triangles) would have fit under a 58k Unity
building finish with no cut. They were left as the raw meshes already in the build, so a building finish
would not flatten or remap them. Their boards are hung props, same as the other stores.

## What the signs say

| store | prop | copy |
| --- | --- | --- |
| store-liquor-blank | liquor-sign (existing board) | LIQUOR · BEER · WINE |
| store-laundromat | laundromat-sign | LAUNDROMAT |
| store-takeout | takeout-sign | CHICKEN · TAKEOUT |
| store-check-cashing | check-cashing-sign | CHECK CASHING |
| store-barber | barber-sign | BARBER (covers the blank white board) |
| shop-auto-garage | auto-sign | TIRES · AUTO |
| shop-gas-station | gas-store-sign, gas-price-sign | GAS · FOOD, and REG 3.49 / PLUS 3.69 / SUPER 3.89 |

The gas signs are props on the current canopy-and-pump mesh. The price sign stands at the pump-island side,
not in front of the kiosk door. The new gas station model is held with the other v5 files.

## Finish

Each new board is a 2-triangle quad, material name `sign_lit`, with a letter-and-halo emission map. They
went through the Sovereign Asset Unity prop preset (`unity-static`): no triangle target, no size change,
color standard and surface maps on. The first pass used an emissive factor of 1.5, which the Khronos
validator rejects (glTF factors are 0 to 1), so the finished files were written but marked not ready. The
factor was then clamped to 1, 0.767, 0.433. Triangles stayed at 2. The existing liquor-sign file was not
run through the finish again; an emission map was added to that already finished file so the letters can
glow.

DayCycle still ramps sign_lit from about 35 percent by day to full at night. PropBuilder.PropMat now keeps
emission on lettered sign_lit (and liquor_sign) boards. The far-tower maps stay dark.

Verdicts: `se-props/u75p/sa/*.verdict.json` (on the box, not in this repo).
