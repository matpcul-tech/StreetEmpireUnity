# u75m: pawn sign lettered, bungalow front redrawn

Two finished buildings replaced. Both were worked on in Sovereign Asset (Sovereign-draft branch
`claude/asset-openings`, on top of `awnings` b7a6717) with `scripts/asset-redraw.mjs` and the plans in this
folder. Input was the v4 file already in the game, byte for byte. Both READY, verdicts here. Not built in Unity here.

| file | bytes | SHA-256 | materials |
| --- | --- | --- | --- |
| store-pawn.glb | 6,423,252 | 2669bd6991e9c4c8eb0793975744fdb90a7bd20f6939934331c51fe6dd9c1dd8 | Material_0, sign_lit, windows_lit, walls |
| house-01-bungalow.glb | 9,671,508 | 31ede8437ad8d2a93fe5cd145141858b30e8746d326f45eec3d798f736228f64 | Material_0, windows_lit, windows_dark, door, trim, walls |

## What changed in the files

- store-pawn: the blank yellow board is now a lettered board, "PAWN · LOANS · GOLD", material `sign_lit`, its own
  texture as base color and emission (factor 1,1,1). The old blank sign face went back to Material_0.
- house-01-bungalow, front only: two stray sheets of old paint that lay across the front wall are gone (the long
  diagonal streaks); the wedge of wall across the top of the door bay is cut away; the torn door is a redrawn paneled
  door (`door`, own texture) in a frame; both front windows are one whole lit pane each (`windows_lit`) behind a
  frame, sill and sash bars; the stained, creased wall under the left window is a clean patch of the wall's own paint.
  Frames, sills and bars are a new material, `trim`, with its own small texture.

## Why this exists

The sign lettering and the clean bungalow front used to live in the Unity build (the lettered board quad over the
pawn shop, tools/rebuild_house01.py). Each time the build went back to the delivered file as is, they were removed
and the flaws in the file came back. Both fixes are in the files now. Nothing in the build should patch either
building.

## For the build

1. Use both files as delivered. No board quad over the pawn sign, no rebuild, no wash, no added panes.
2. Re-extract textures for these two models only:
   `node tools/extract_building_textures.mjs Assets/Art/Models/Buildings Assets/Art/Textures/Buildings store-pawn`
   and the same with `house-01-bungalow`. New files to expect: `house-01-bungalow__trim_basecolor.jpg`, and
   `house-01-bungalow__door_basecolor.jpg` and `store-pawn__sign_lit_basecolor.jpg` change.
3. `trim` is a plain lit material: BindTextures already handles a part name it does not know by loading
   `{name}__trim_basecolor.jpg`. Check the frames are not left untextured.
4. Update the sizes and hashes in the SceneBuilder.cs comment block for the two files.
5. Shots to send back: 01 bungalow front near, 03 pawn front near, both at 22:00 and by day.

## Known and left alone

- The bungalow's right side wall still has the generator's rough barred windows. Only the street front was asked for.
- The porch's left side wall keeps its old grimy paint.
