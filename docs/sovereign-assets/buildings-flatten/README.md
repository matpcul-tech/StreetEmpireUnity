# Sovereign Asset: Flatten walls rerun of the Eastside kit buildings (Nov 17, 2026)

Matthew's v71 phone notes: walls looked shredded and crumpled (yellow barbershop, white house with the red roof,
pawn shop, others). Every Meshy kit building placed in the Eastside went back through Sovereign Asset with the
new Flatten walls setting.

Sovereign Asset build: branch claude/asset-flatten-walls, commit 9ad9173 (not merged; built from the worktree at
/workspace/sa-grayblock/sa-flatten and served locally). Driver: tools/sa-flat.mjs (sa-finish.mjs plus the
Flatten walls control), pipeline tools/flat_lods.sh and tools/win_pass.sh.

Steps per building, preset Street Empire building (se-building):
1. full: the READY source with Flatten walls Light, and again with Strong. Strong is kept where it pressed the
   walls further (its largest point move clearly above Light's, so Light had left dents) and stayed READY.
   store-pawn uses the Light file already made by the other worker
   (/workspace/sa-grayblock/flatten-light/store-pawn-se-building-light.glb); Strong was byte identical there.
2. near: se-building with a 26,000 triangle cut of the kept file (same flatten level). house-03-duplex keeps its
   full file as near, as before (its cut came back NOT READY earlier).
3. far and low: Blender collapse decimate of the kept file (22 and 6 percent, tools/decimate_far.py), then a no
   cut se-building pass; a second no cut pass where the first was NOT READY (house-03-duplex far).
4. final (the file Unity takes its textures from): tools/window_maps.mjs added window glass (low roughness) and
   dim warm lit rooms (emissive, about two in five window bays lit) as glTF textures, then a no cut se-building
   pass at the same flatten level.

Verdicts and reports per level are in full/, near/, far/, low/, final/. Every file below is READY.

| Building | flatten | full | final (windows) | near | far | low |
|---|---|---|---|---|---|---|
| store-barber | Strong | 57983 | 57973 | 25991 | 12770 | 3460 |
| house-01-bungalow | Light (Strong moved no further) | 58928 | 58928 | 26362 | 12988 | 3554 |
| store-pawn | Light | 58000 | 57997 | 25997 | 12806 | 3471 |
| house-02-two-story | Light | 58000 | 58000 | 26000 | 12855 | 3485 |
| house-03-duplex | Strong | 40429 | 40429 | 40429 | 8905 | 2425 |
| store-bodega | Light | 57995 | 57995 | 30204 | 12766 | 3477 |
| store-laundromat | Strong | 58000 | 58000 | 25992 | 12801 | 3484 |
| store-takeout | Light | 58000 | 58000 | 25997 | 12864 | 3482 |
| shop-auto-garage | Strong | 57996 | 57996 | 35665 | 12763 | 3483 |
| shop-gas-station | Light | 58000 | 58000 | 29556 | 12793 | 3367 |
| civic-church | Light | 58000 | 58000 | 25999 | 12764 | 3480 |
| apt-01-stucco | Light | 57999 | 57999 | 39071 | 12754 | 3393 |
| apt-02-brick | Strong | 57999 | 57999 | 49042 | 12762 | 3320 |
| apt-mixed-use | Light | 58000 | 58000 | 26000 | 12791 | 3481 |
| apt-motel-style | Light | 57997 | 57997 | 29820 | 12769 | 3479 |

Not rerun: store-liquor-blank (1,572 triangle low poly, no crumple) and the four houses no longer placed
(house-wood-duplex, house-shotgun, house-boarded, house-rowhouse-pair). Flatten does not repaint the maps, so
crumple baked into a texture stays; Unity draws the building normal maps at 0.55 strength for that reason.
