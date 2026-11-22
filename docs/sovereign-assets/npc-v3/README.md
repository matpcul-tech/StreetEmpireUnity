# NPC v3 in the game (Nov 22, 2026)

Source: release `meshy-npc-v3-2026-11-22`, eight GLBs, all verified against MANIFEST.txt (SHA-256). They replace the
eight characters of the same names: player, unc, cop-m-1, cop-m-2, ped-m-1, ped-m-2, ped-f-1, ped-f-2.

## What was changed in the files before converting

| Character | Change |
|---|---|
| player, unc | None. Converted from the delivered bytes. |
| cop-m-1, cop-m-2, ped-m-1, ped-m-2, ped-f-1, ped-f-2 | Skin warmed in the base color: the pale gray-pink skin moved to a warm light tone (hue about 13 to 19 degrees up to 17 to 20, saturation 0.17 to 0.25 up to 0.30 to 0.36, a little darker). Only skin texels change: texels on triangles that follow the head, neck, hands (and bare arms), close in color to the face's own skin, plus the padding round those islands so no pale seam shows. Numbers per character in skin_report.json; faces before and after in skin-before-after.jpg. |
| ped-m-1 | Two triangles at the crotch had one corner's UV far across the atlas (154 and 181 times the usual stretch). Each got a vertex of its own with the UV its shape calls for. |

Mesh, rig, weights, normal and roughness maps are as delivered, apart from those two triangles on ped-m-1.

## What was looked at and left alone

- **cop-m-1, the line on his jaw.** It is not paint. The base color there is clean skin. It is a shading crease:
  the model has per-face normals and its normal map carries the smoothing, and along one edge on the jaw (two
  faces about 17 degrees apart) the map does not hide the step. Retouching the texture did nothing, smoothing the
  normal map there did nothing, and welding the normals made new creases all over the face. It needs a re-bake on
  the art side, or it stays. At play distance it is a hairline.
- **ped-f-1, the hair.** Turned her head 55 degrees each way and nodded 25: the hair twists but does not cut through
  the shoulders, because the lower hair already follows the shoulder bones. Moving it to the spine was tried and
  dropped: the shirt then pokes through the hair when the shoulders lift.
- **Arms out in the walk, and Unc's long stride.** Both are from the sheet's preview walk. The game does not play
  that walk: tools/tpose_fbx.py makes a T-pose the rest pose and Unity Humanoid plays the game's own clips on it
  (converted-tpose.jpg shows the eight after conversion). Unc stands in the game; he does not walk yet. Not checked
  in a running game from here.

## In the game

- Converted with the repo's tools (tools/glb_to_fbx.py, tools/tpose_fbx.py, tools/extract_textures.mjs): T-posed FBX
  plus 1024 base color and normal JPEGs, same paths as before.
- Heights: every v3 file is 1.80 m. GameBuilder now brings each to its own height (cops 1.82 and 1.76, peds 1.78,
  1.66, 1.72, 1.70); before it only rescaled a file more than a tenth off, which would have left all eight at 1.80.
- The scripts used for the fixes are in tools/npc-v3.
- These 24 character files and the two sheets here are plain git files, not LFS (see .gitattributes).

Not done: more pedestrians. Four is a thin crowd, and that needs new characters from the art side.
