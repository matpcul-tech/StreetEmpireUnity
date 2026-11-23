# r2: remeshed wave A and B buildings, batch 1 (7 of 18)

Source: prerelease `meshy-buildings-wave-ab-r2-2026-11-23`. The GLBs here match its MANIFEST.txt byte for byte (SHA-256).

Why: the as delivered files are 81k to 1.1M triangles and 21 to 64 MB each. They pushed the APK from 266 MB to 546 MB.

Method: a Meshy remesh (never a decimation) of each original model, scaled to the as delivered height, origin at ground
center, same facing and footprint, so each file drops into its old lot with no code change. Material names, count and
order are the same as before (windows_lit keeps its emissive factor). Each file has one 1024 atlas; the extracted
textures (`NAME__Material_0_*`) were rebuilt with tools/extract_building_textures.mjs, door copies not kept as before.

| model | triangles before | triangles now | MB before | MB now |
|---|---|---|---|---|
| civic-church | 96,044 | 66,297 | 39.6 | 3.81 |
| tower-copper-mansard | 83,279 | 57,522 | 41.2 | 4.64 |
| crew-redkings-club | 214,254 | 25,658 | 29.9 | 1.98 |
| civic-clinic | 80,754 | 23,182 | 26.3 | 1.82 |
| dock-ice-house | 249,464 | 25,846 | 33.5 | 1.88 |
| job-pool-hall | 88,972 | 23,923 | 35.9 | 1.89 |
| job-loading-lot | 101,064 | 20,695 | 24.5 | 1.92 |

The other 11 wave A and B buildings are still the as delivered files and follow in a later PR. The "Add swap (large
building imports)" CI step stays until all of them are in.
