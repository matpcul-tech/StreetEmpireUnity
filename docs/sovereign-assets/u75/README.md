# v75 assets through Sovereign Asset (main d9dcc22)

New or changed models, decals and textures for v75. Buildings use the "Unity building" preset (unity-building) with
Size set to the real height; props, the edge, signs and the fight dust use "Unity prop" (unity-static).

- house-01-bungalow: rebuilt by tools/rebuild_house01.py. Only the roof of the v74 SA output is kept (every face above
  the 2.2 m eave); walls, porch, posts, windows and door are new clean boxes and planes on a tiling white stucco. No
  Meshy spend. Far and Low: Blender collapse decimate, then Unity prop with Fix normals.
- palm-tall, palm-short: tools/model_palms.py, thicker tapered leaflets at twice the texture size, five segment
  fronds with about twice the droop; alpha bled (tools/alpha_bleed.mjs) before SA.
- street-blade-0 to 7: the back face U flipped (gltf-tools/flip_back_u.mjs) so it no longer reads mirrored.
- edge-freeway, edge-tower-a to d: tools/model_edge.py, the freeway ring and the stacked far skyline (lit window
  emission map).
- pawn-sign: a painted board quad, "PAWN  LOANS  GOLD". fx-dust: the fight dust and grit sprite.

| Asset | Group | Preset | Verdict | Triangles | Windows |
|---|---|---|---|---|---|
| street-blade-0 | blades-sa | unity-static | READY | 12 |  |
| street-blade-1 | blades-sa | unity-static | READY | 12 |  |
| street-blade-2 | blades-sa | unity-static | READY | 12 |  |
| street-blade-3 | blades-sa | unity-static | READY | 12 |  |
| street-blade-4 | blades-sa | unity-static | READY | 12 |  |
| street-blade-5 | blades-sa | unity-static | READY | 12 |  |
| street-blade-6 | blades-sa | unity-static | READY | 12 |  |
| street-blade-7 | blades-sa | unity-static | READY | 12 |  |
| edge-freeway | edge-sa | unity-static | READY | 72 |  |
| edge-tower-a | edge-sa | unity-static | READY | 10 |  |
| edge-tower-b | edge-sa | unity-static | READY | 10 |  |
| edge-tower-c | edge-sa | unity-static | READY | 10 |  |
| edge-tower-d | edge-sa | unity-static | READY | 10 |  |
| house-01-bungalow | house-far | unity-static | READY | 12800 |  |
| house-01-bungalow | house-low | unity-static | READY | 3499 |  |
| house-01-bungalow | house-near | unity-building | READY | 48938 | None found. Only glass that is set back from its frame in the mesh can be found. |
| palm-short | palm-sa | unity-static | READY | 160 |  |
| palm-tall | palm-sa | unity-static | READY | 160 |  |
| fx-dust | quads-sa | unity-static | READY | 2 |  |
| pawn-sign | quads-sa | unity-static | READY | 2 |  |
| liquor-sign | u75c | unity-static | READY | 2 | n/a |
| sedan | u75c rebake (tools/rebake_car.py) | unity-static | READY | 29996 | 270 px/m (was 105) |
| sedan LOD1 | u75c rebake, Blender decimate 0.2 | unity-static | READY | 5996 | 267 px/m |
| police | u75c rebake (WELD 2e-5, MARGIN 0.0015) | unity-static | READY | 30530 | 924 px/m in file |
| police LOD1 | u75c rebake, decimate 0.25 | unity-static | READY | 7645 | 908 px/m in file |
| edge-tower-a to d | u75c: base color 40% toward the day haze (186,196,212), gltf-tools/haze_tex.mjs | unity-static | READY | 10 each | 24 px/m |

## v75d: Matthew's v2 finished files (Sovereign Assets upgraded)

house-01-bungalow (Material_0, windows_lit, windows_dark, door, walls), apt-01-stucco (same five) and store-pawn
(Material_0, sign_lit, windows_lit, walls) replace the earlier versions as they are, only scaled in Unity (4, 10 and
4.5 m). Near: checked with Unity building at Size Height, the GLB in the game is Matthew's file. Far (0.22) and Low
(0.06): Blender collapse decimate (house Low with RECALC=1, one shell came out inside out), then Unity prop with Fix
normals. Verdicts in u75d/. No Meshy spend.

| Asset | Group | Preset | Verdict | Triangles | Notes |
|---|---|---|---|---|---|
| house-01-bungalow | u75d near | unity-building | READY | 58928 | 99 px/m, color PASS |
| house-01-bungalow | u75d far | unity-static | READY | 12964 | |
| house-01-bungalow | u75d low | unity-static | READY | 3535 | first pass NOT READY (inside out shell), fixed by RECALC |
| apt-01-stucco | u75d near | unity-building | READY | 57999 | 22 px/m, color PASS |
| apt-01-stucco | u75d far | unity-static | READY | 12759 | |
| apt-01-stucco | u75d low | unity-static | READY | 3479 | |
| store-pawn | u75d near | unity-building | READY | 58000 | 70 px/m, color PASS |
| store-pawn | u75d far | unity-static | READY | 12760 | |
| store-pawn | u75d low | unity-static | READY | 3480 | |

## v75e: the white house's normals, roof material and front panes (tools/house01_v2_fix.py)

Matthew's v2 house-01-bungalow had its vertex normals smoothed across the wall and roof edges (39% of the wall
triangles bent up toward the sky), so the walls caught the sky light and moonlight and read almost as bright at night
as by day. The custom normals are cleared and smoothed by angle (30 degrees). The up-facing faces of its walls material
(the roof) move to a "roof" material on the same textures, so the walls can be tinted per lot without the roof. The
two front windows were painted only, so each gets a windows_lit pane. Far and Low decimated again from it. No Meshy spend.

| Asset | Group | Preset | Verdict | Triangles |
|---|---|---|---|---|
| house-01-bungalow | u75e near | unity-building | READY | 58932 |
| house-01-bungalow | u75e far | unity-static | READY | 12965 |
| house-01-bungalow | u75e low | unity-static | READY | 3535 |
