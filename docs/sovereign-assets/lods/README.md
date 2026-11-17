# Sovereign Asset: stage 2 LODs (Nov 17, 2026)

Every lower detail mesh added in stage 2 went through Sovereign Asset (the real app, /asset.html, driven by
se-props/sa-finish.mjs with Fix normals and Fix inside-out faces on). The verdict and report for each file are
in the folders here.

## Props, LOD1 (preset Game ready, triangle target set per prop, 512 px textures)

When the first pass came back NOT READY (it stopped above the target with "going lower would visibly change
the shape", or a check failed after the cut) the first pass output went through a second pass with no cut,
and that verdict is the one saved. The trees are left without a LOD1: Sovereign Asset only got them from
2,044 to about 1,800 triangles, not worth a second mesh. Only the LOD1 meshes are used; their textures are
not (the props keep their full materials).

| Prop | LOD0 tris | LOD1 tris | first pass | final |
|---|---|---|---|---|
| aircon | 2998 | 1526 | NOT READY | READY |
| barrel | 1476 | 499 | READY | READY |
| cardboard-box | 1000 | 297 | READY | READY |
| chainlink-panel | 3026 | 800 | READY | READY |
| crate | 2000 | 1166 | NOT READY | READY |
| hydrant-aged | 3695 | 1704 | NOT READY | READY |
| hydrant | 3695 | 1704 | NOT READY | READY |
| manhole | 1499 | 1083 | NOT READY | READY |
| road-barrier | 2497 | 699 | READY | READY |
| security-light | 1496 | 930 | NOT READY | READY |
| street-lamp-1 | 6002 | 1484 | NOT READY | READY |
| trash-can-rust | 2996 | 1002 | NOT READY | READY |
| trash-can | 2996 | 950 | NOT READY | READY |
| trashbag | 1500 | 498 | READY | READY |
| tyre | 1500 | 500 | READY | READY |
| utility-box-1 | 2000 | 600 | READY | READY |
| utility-box-2 | 2500 | 597 | READY | READY |

## Parked cars, LOD1 (preset Game ready, 6,000 target, 1024 px)

The Street Empire car preset (se-car) is the kit assembly step (splits the wheels, builds the window mask) and
has no triangle setting, so the parked car LOD1 used Game ready on the finished kit cars.

| Car | LOD0 tris | LOD1 tris | first pass | final |
|---|---|---|---|---|
| sedan | 29996 | 5995 | READY | READY |
| police | 30484 | 7729 | NOT READY (inside-out shell, normals, stopped at 7,728 of 6,000) | READY (second pass, no cut) |

## Buildings: near, far, low

Source: the 20 Sovereign-READY se-building files (docs/sovereign-assets/buildings; house-01 and house-03
from their READY second pass).
- Near: Sovereign Asset se-building with a 26,000 triangle cut (its own simplifier). Where it stopped above
  26k it said going lower would visibly change the shape; those files keep the count it reached.
- Far and Low: Sovereign Asset will not cut these Meshy meshes below about 25k, so the steps that applied
  are a Blender collapse decimate (tools/decimate_far.py, 22% for far, 6% for low) of the READY file, then a
  Sovereign Asset se-building pass with no cut (cleanup and checks), and a second no-cut pass where the first
  was NOT READY.
- house-03-duplex near: the 26k cut came back NOT READY twice (one shell inside out that the fix did not
  clear), from both the first and the second pass source. Its near model is the full READY file
  (40,429 triangles) instead. Verdicts: buildings-near/house-03-duplex.cut-attempt*.verdict.json.
- house-03-duplex far and low were decimated from its first pass file; both came back READY on their own.
- store-liquor-blank is 1,572 triangles and has no far or low level.

| Building | source tris | near | far | low |
|---|---|---|---|---|
| apt-01-stucco | 57999 | 38671 READY | 12754 READY | 3394 READY |
| apt-02-brick | 57999 | 49010 READY | 12762 READY | 3320 READY |
| apt-mixed-use | 58000 | 26000 READY | 12840 READY | 3481 READY |
| apt-motel-style | 57997 | 29220 READY | 12768 READY | 3479 READY |
| civic-church | 58000 | 26000 READY | 12765 READY | 3480 READY |
| house-01-bungalow | 58928 | 26368 READY | 12989 READY | 3554 READY |
| house-02-two-story | 58000 | 25999 READY | 12855 READY | 3484 READY |
| house-03-duplex | 40429 | 40429 see note | 8903 READY | 2425 READY |
| house-boarded | 57999 | 25998 READY | 12802 READY | 3483 READY |
| house-rowhouse-pair | 58000 | 28032 READY | 12771 READY | 3406 READY |
| house-shotgun | 58000 | 25998 READY | 12796 READY | 3477 READY |
| house-wood-duplex | 58000 | 26000 READY | 12781 READY | 3465 READY |
| shop-auto-garage | 57998 | 35170 READY | 12762 READY | 3483 READY |
| shop-gas-station | 58000 | 29268 READY | 12777 READY | 3364 READY |
| store-barber | 58000 | 26622 READY | 12774 READY | 3471 READY |
| store-bodega | 57995 | 30361 READY | 12760 READY | 3476 READY |
| store-laundromat | 58000 | 25996 READY | 12822 READY | 3485 READY |
| store-pawn | 58000 | 26000 READY | 12800 READY | 3469 READY |
| store-takeout | 58000 | 25998 READY | 12950 READY | 3488 READY |
