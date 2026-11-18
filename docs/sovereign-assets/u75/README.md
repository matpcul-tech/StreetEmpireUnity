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
