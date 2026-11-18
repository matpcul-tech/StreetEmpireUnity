# v74 buildings through Sovereign Asset

Sovereign Asset main d9dcc22. The near models used the real **"Unity building"** preset (id `unity-building`: Flatten
walls, window lights split into windows_lit and windows_dark, surface maps, color standard, 2048 textures), with
Size set to the real height (height axis, meters). Earlier SA builds had no such preset; `unity-static` is now named
"Unity prop". The Far and Low levels are a Blender collapse decimate of the near file, then SA "Unity prop" with Fix
normals and holes off (a second pass where needed). Each material got a slightly different base color factor before
that pass, so SA does not merge the texture-less walls slot into Material_0.

apt-01-stucco and store-pawn are Matthew's finished files, used as they are and scaled only in Unity (10 m, 4.5 m).

Heights are applied in Unity (SceneBuilder.RealHeight). apt-motel-style is 7 m, not 10: at 10 m it would be about
20 m wide, wider than its 13 m lot.

| Building | Height m | Flatten | Near | Far | Low | Windows | Shipped |
|---|---|---|---|---|---|---|---|
| apt-01-stucco | 10 | n/a | Matthew's finished file, used as is (not re-run) | READY 12785 | READY 3425 | Matthew's file: windows_lit and windows_dark | yes |
| apt-02-brick | 10 | strong | READY 50022 | READY 12800 | READY 3413 | 62 windows: 42 lit (827 triangles, material windows_lit) and 20 dark (390 triangles, material windows_dark). | yes |
| apt-mixed-use | 10 | light | READY 26216 | READY 12800 | READY 3500 | 9 windows: 7 lit (67 triangles, material windows_lit) and 2 dark (26 triangles, material windows_dark). | yes |
| apt-motel-style | 7 | light | READY 31998 | READY 12800 | READY 3500 | 16 windows: 10 lit (421 triangles, material windows_lit) and 6 dark (222 triangles, material windows_dark). | yes |
| civic-church | 10 | light | READY 26116 | READY 12800 | READY 3495 | None found. Only glass that is set back from its frame in the mesh can be found. | yes |
| house-01-bungalow | 4 | strong | NOT READY 51967 | not made | not made | 7 windows: 3 lit (19 triangles, material windows_lit) and 4 dark (37 triangles, material windows_dark). | no (NOT READY, listed for a Meshy regen) |
| house-02-two-story | 7 | light | READY 26000 | READY 12800 | READY 3496 | 3 windows: 3 lit (138 triangles, material windows_lit) and 0 dark (0 triangles, material windows_dark). | yes |
| house-03-duplex | 4 | strong | READY 33972 | READY 12800 | READY 3500 | 3 windows: 3 lit (50 triangles, material windows_lit) and 0 dark (0 triangles, material windows_dark). | yes |
| house-boarded | 7 | light | READY 25998 | READY 12800 | READY 3500 | 3 windows: 2 lit (58 triangles, material windows_lit) and 1 dark (30 triangles, material windows_dark). | yes |
| house-rowhouse-pair | 9 | light | READY 32170 | READY 12800 | READY 3417 | 22 windows: 10 lit (786 triangles, material windows_lit) and 12 dark (870 triangles, material windows_dark). | yes |
| house-shotgun | 4.5 | light | READY 26063 | READY 12800 | READY 3498 | 10 windows: 7 lit (347 triangles, material windows_lit) and 3 dark (98 triangles, material windows_dark). | yes |
| house-wood-duplex | 7.5 | light | READY 26010 | READY 12800 | READY 3489 | 17 windows: 8 lit (645 triangles, material windows_lit) and 9 dark (193 triangles, material windows_dark). | yes |
| shop-auto-garage | 5 | strong | READY 38242 | READY 12800 | READY 3500 | 9 windows: 7 lit (716 triangles, material windows_lit) and 2 dark (101 triangles, material windows_dark). | yes |
| shop-gas-station | 5 | light | READY 33617 | READY 12692 | READY 3334 | 5 windows: 4 lit (93 triangles, material windows_lit) and 1 dark (47 triangles, material windows_dark). | yes |
| store-barber | 4.5 | strong | READY 30014 | READY 12800 | READY 3496 | 3 windows: 2 lit (63 triangles, material windows_lit) and 1 dark (23 triangles, material windows_dark). | yes |
| store-bodega | 7 | light | READY 33561 | READY 12798 | READY 3498 | 5 windows: 1 lit (27 triangles, material windows_lit) and 4 dark (107 triangles, material windows_dark). | yes |
| store-laundromat | 6 | strong | READY 58000 | READY 12800 | READY 3500 | 4 windows: 3 lit (1039 triangles, material windows_lit) and 1 dark (127 triangles, material windows_dark). | yes |
| store-liquor-blank | 4.9 | light | READY 1572 | READY 1411 | READY 786 | None found. Only glass that is set back from its frame in the mesh can be found. | yes |
| store-pawn | 4.5 | n/a | Matthew's finished file, used as is (not re-run) | READY 12800 | READY 3487 | Matthew's file: windows_lit, no windows_dark | yes |
| store-takeout | 7.5 | light | READY 26019 | READY 12799 | READY 3500 | 4 windows: 3 lit (470 triangles, material windows_lit) and 1 dark (81 triangles, material windows_dark). | yes |

Windows "None found": civic-church and store-liquor-blank. Their windows are left as Meshy made them (no glow).

house-01-bungalow (the white house with the red roof): NOT READY on every run ("1 shell is inside out"), with
Flatten Strong, Fix normals, holes off and no triangle target. Not shipped; listed for a Meshy regeneration. The
house slots now alternate house-02-two-story and house-03-duplex.

Traffic signal (props): lenses moved in front of the visor caps (they were hidden, so the signals read as dark
boxes), then SA "Unity prop" with Fix normals: READY, 925 triangles.

Verdict images are shrunk to 900 px JPEG here; the full reports are next to them.
