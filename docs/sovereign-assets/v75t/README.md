# v75t: wave A civic and wave B buildings in the town

Source: release `meshy-buildings-wave-ab-2026-11-22` (5 civic from wave A, 15 from wave B), finished in Sovereign Assets
(engine main a642bef) with Matthew's as-delivered rule. Checksums match the release SHA256SUMS. The GLBs are copied byte
for byte: no decimation, no LOD, no simplified copies at any distance, no wall paint, no wash, no tint, no added window
quads. civic-church.glb is replaced by the wave A church.

## Textures

Each file has one Meshy atlas (image 0, 4096 JPEG) that its unnamed main material, its door and its windows_lit and
windows_dark all sample. `tools/extract_building_textures.mjs <dir> <texdir> NAME` pulls `NAME__Material_0_basecolor.jpg`
(1024, like every other building trim atlas), `_normal.jpg` and
`_metalsmooth.png`; the door copies were not kept (SceneBuilder binds the Material_0 set to every part of these files).
Only windows_lit glows (the file's own emissive factor, ramped by DayCycle at night). windows_dark is matte and dark.

## Placement (block.lot)

Landmark lots override the dealt model (the deal still counts the lot, so no other lot changes). A file wider or deeper
than the 13 m lot keeps its outer edges on the block edge and grows toward the block middle. PropBuilder does not place a
yard prop or a yard parked car that would land inside a landmark. All landmark lots are in the sixteen new blocks, away
from the nine turf blocks and the gas and auto shop blocks; the church keeps its old store lot on block 20.

| model | triangles | bytes | sha256 (first 16) | lot |
|---|---|---|---|---|
| civic-bank | 146,570 | 29,284,424 | bd5b47ecedb783f0 | 30.3 |
| civic-church (remesh) | 96,044 | 39,567,716 | fbbe09e54ce9d8c4 | 20.0 (the store lot the old church had) |
| civic-clinic | 80,754 | 26,318,364 | cf17a2ba7ac2f13d | 27.2 |
| civic-police-station | 184,272 | 32,272,244 | e4fd5043a67b9696 | 30.2 |
| civic-school | 86,872 | 26,914,280 | 7e4415f4a5f84fa7 | 28.3 |
| crew-bluehand-club | 155,340 | 30,705,768 | 6819f3c84db7704f | 13.3 |
| crew-goldline-harbor | 240,632 | 33,007,752 | 9979e8c814df11b3 | 22.3 |
| crew-redkings-club | 214,254 | 29,882,856 | 799e28cc3f21b74f | 21.3 |
| dock-ice-house | 249,464 | 33,546,668 | ffd8c220ef772f92 | 31.2 |
| dock-pier-warehouse (remesh) | 84,618 | 36,155,484 | 521e0e290b471552 | 31.1 |
| job-car-wash | 48,908 | 22,086,076 | 86abfac2176aa2a7 | 39.2 |
| job-chop-shop | 93,874 | 24,407,424 | f1237dd5692de304 | 39.3 |
| job-loading-lot | 101,064 | 24,517,244 | beb899087f71e3a0 | 31.0 |
| job-pool-hall (remesh) | 88,972 | 35,909,620 | 937fb213041a655f | 37.3 |
| tower-brutalist-concrete | 128,486 | 29,066,240 | e3508409a7180ccf | 40.3 |
| tower-copper-mansard (remesh) | 83,279 | 41,200,808 | 3616fc6f7485141b | 40.2 |
| tower-deco-setback | 514,798 | 46,207,860 | d48c5787bca68ef1 | 40.1 |
| tower-terracotta | 1,110,556 | 64,323,196 | c85b30df963b2902 | 40.0 |
| warehouse-cold-storage | 44,728 | 21,131,624 | b0315f6b157aef1c | 31.3 |
| warehouse-sawtooth (remesh) | 92,761 | 34,972,652 | 3a1654de530b778a | 39.1 |

## Heavy files

Five of the seven heavy files are Matthew's Meshy remesh at about 85k to 96k triangles, finished with the same
Sovereign Assets rules and passed in review (heavy-remesh/finished, SHA256SUMS matched): civic-church 96,044,
tower-copper-mansard 83,279, dock-pier-warehouse 84,618, warehouse-sawtooth 92,761, job-pool-hall 88,972. Same height,
footprint and material layout as the earlier files. tower-terracotta (1,110,556) and tower-deco-setback (514,798) stay
as delivered: their remeshes had shard panes across the windows and were kept out. Both stand on Downtown block 40,
so a view of Downtown still draws about 1.75M building triangles from the four towers.
