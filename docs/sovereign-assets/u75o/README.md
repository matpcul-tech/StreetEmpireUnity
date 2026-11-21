# u75o: house-03-duplex redrawn and ready to go back in the rotation

This is the pale stucco house with the gray hip roof, two front doors, the AC unit and the porch railing. It was taken
out of the rotation at v75j "until a finished one arrives". This is that file.

| file | bytes | SHA-256 | materials |
| --- | --- | --- | --- |
| house-03-duplex.glb | 3209036 | 23752832308e0ae794ae6eee04b2071e90e492461f3805a5ed3dc9cad842b5dc | Material_0, trim, bars, windows_lit |

Worked on in Sovereign Asset (Sovereign-draft branch `claude/asset-ribbon`) with `scripts/asset-redraw.mjs` and
`house-03-duplex.plan.json`. Input was the file already in this repo. READY, verdict here. Not built in Unity here.

## What changed in the file

- Front: both windows redrawn as whole lit panes with frames (`trim`). The right one has straight iron bars
  (`bars`). The left one keeps its AC unit and has no bars. The torn glass and bent bars are gone.
- Back: the long window that was torn into triangles is one ribbon window of four sashes, lit.
- Windows carry rooms the same way as u75n: `windows_lit` has `windows_day` as base color and `windows_night` as
  its emission map, emissive factor 1,1,1.
- Walls, roof, doors, porch and railing are the original model, untouched. This is still the one-texture atlas
  model (Material_0, 1024 px), not a walls/roof split.

## For the build

1. Put house-03-duplex back in the house rotation (SceneBuilder.cs, the "house" list, and remove the v75j note).
2. Treat it as a finished file: add it to the finished set so its windows_lit keeps its own base and emission maps
   and nothing tints, washes or re-lights it. Drop the old light wash this model used to get
   (the "one atlas model (house-03-duplex) gets the old light wash" branch).
3. Re-extract its textures: `node tools/extract_building_textures.mjs Assets/Art/Models/Buildings Assets/Art/Textures/Buildings house-03-duplex`.
   New: `house-03-duplex__trim_basecolor.jpg`, `house-03-duplex__bars_basecolor.jpg`.
4. The Low and Far copies of this model are the old ones. Regenerate them from this file or they will show the torn
   back window at a distance.
5. Shots to send back: front and back, 14:00 and 22:00.

## Known and left alone

- A thin stray sliver hangs across the top of the right front door. It is not on a wall, so the redraw did not reach it.
- The model's own texture is 1024 px, softer than the buildings finished from new Meshy files.
