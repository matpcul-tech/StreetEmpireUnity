# v75l: seven new Eastside buildings through Sovereign Asset (main a642bef)

Source: release `meshy-new-buildings-2026-11-19`, untouched Meshy exports, all seven verified against MANIFEST.txt (SHA-256).
The release sheet shows an eighth building, store-tire-shop; it is not in the release, so it is not in this set.

Run: `tools/sa-node.mjs` (the app's own `src/assetapp/engine.js` in Node, sharp for image decode and encode, the bundled
Poly Haven scans, the Khronos validator). Preset "Unity building", Size Height, triangle target 58,000 (the two story
duplex 70,000: the app refused to go lower). Four files came out NOT READY on the first pass (inside-out shells or
wrong-side normals after the triangle cut); `tools/sa-node-pass2.mjs` ran the finished file through again with only
Fix inside-out faces and Fix normals, the same recipe as docs/sovereign-assets/buildings. All seven are READY.

Two changes after the finish, material fields only, no mesh, UV or texture change: the unnamed trim material is named
`Material_0` (the name the game and tools/extract_building_textures.mjs expect), and apt-dingbat's `sign_lit` has its
emission switched off (the two panels it found are blank pale wall panels, which would have glowed at night).

| Asset | Height | Footprint | Source triangles | Finished | Windows | Doors | Walls px/m | Color | Verdict |
|---|---|---|---|---|---|---|---|---|---|
| house-spanish-tile | 4.5 m | 6.5 x 5.8 m | 1,366,264 | 58,483 | 3 lit, 2 dark | 1 | 151 | PASS | READY (pass 2) |
| house-craftsman-porch | 4.8 m | 7.7 x 8.5 m | 427,412 | 58,431 | 2 lit, 1 dark | 0 | 107 | PASS | READY |
| house-stucco-duplex-2story | 6.5 m | 6.2 x 4.6 m | 1,665,224 | 70,236 | 2 lit, 2 dark | 4 | 133 | PASS | READY (pass 2) |
| house-boarded-stucco | 4.2 m | 7.2 x 5.8 m | 1,290,496 | 57,890 | none (boarded) | 2 | 154 | PASS | READY (pass 2) |
| house-flat-roof | 3.6 m (drawn at 4 m) | 4.2 x 3.8 m | 540,568 | 58,039 | 2 lit, 2 dark | 0 | 172 | PASS | READY |
| apt-dingbat | 6.5 m | 9.2 x 8.8 m | 153,182 | 58,239 | 1 lit, 1 dark | 0 | 106 | PASS | READY |
| store-check-cashing | 4.5 m | 8.9 x 5.1 m | 141,644 | 57,985 | none found (painted glass) | 1 | 115 | PASS | READY (pass 2) |

In the game: the five houses join house-02-two-story and house-01-bungalow, so the seven house slots are seven models.
store-check-cashing has store-bodega's lot and apt-dingbat has apt-motel-style's lot (Eastside has 9 store lots and 4
apartment lots, all taken); both older files stay in the repo. All seven are in `SceneBuilder.Finished`: scaled and
their maps wired, no wash, no LODs. Renders of the finished files: finished-a.jpg, finished-b.jpg (Blender Cycles, not
the game). The seven GLBs, their textures and these sheets are plain git files, not LFS (see the last block of .gitattributes).
Not run here: Unity. The scene is built by SceneBuilder in CI, so the first look in game is the CI build.
