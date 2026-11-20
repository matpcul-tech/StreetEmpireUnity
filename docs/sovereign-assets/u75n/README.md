# u75n: rooms behind every window, and the white house done on all four sides

Eleven finished buildings replaced. All worked on in Sovereign Asset (Sovereign-draft branch `claude/asset-rooms`)
with `scripts/asset-redraw.mjs`. Input was the file already in the game. All READY, verdicts here. Not built in
Unity here. house-boarded-stucco and store-check-cashing have no window panes and are unchanged.

| file | bytes | SHA-256 | panes |
| --- | --- | --- | --- |
| store-pawn.glb | 6221616 | 08c2603b95b1d8f5c2533da38fd00d168c827e39e86c255441ca9872c1eedb3b | 4 |
| house-01-bungalow.glb | 7984464 | a7991f266152283d7a645fde475bee21baed129fa0124061ff512a0cc9d97e1f | 11 |
| apt-01-stucco.glb | 5718764 | 5972b40481486552828142e5a32a9a819faf62f93d4317992c94ef915754c3cf | 46 |
| house-02-two-story.glb | 5231296 | 5f53253f1d6eb071209ade015baa08e6d9886b541a0cd21bf969b8df041fe475 | 3 |
| apt-mixed-use.glb | 5612804 | b75e17aaacc19d573f477c2cc4918dd7f869616fe536936c223153f23cb8c81c | 9 |
| store-barber.glb | 8799264 | df9fd2071d4a7e9e01ecfd2336ecda40e1f1312046ed4620231c80ce8d12875d | 3 |
| house-spanish-tile.glb | 8600696 | 2a5626663236a176106b3b3088f1419697c16c9d7475e70c417f0cd1d66ea9f0 | 5 |
| house-craftsman-porch.glb | 8164820 | 9dead538c2ecf2f055efd27319145b9fba85770b3418545f1154965274f8ae92 | 5 |
| house-stucco-duplex-2story.glb | 8509276 | bbea5573368c211159c07d2544b5f9ff90ffb95c0244387b176eb9df7182a2a0 | 4 |
| house-flat-roof.glb | 7800828 | 9bb7bfb43bd23365fe809a223fa0f1a5dc415d6c51267cb5cfbd99894191fe37 | 4 |
| apt-dingbat.glb | 7066688 | 6027c60d81addc3d9af232e354b1db79ee81b8ebd2c419fbfcb41748fea07156 | 3 |

## What changed in the files

- Every building above: each window's glass (its sashes together) is now one clean rectangle. `windows_lit` has a
  base color texture (`windows_day`: dark glass, sky in it, the blind or curtain seen through) and an emission
  texture (`windows_night`: the room glowing behind curtains, blinds, shop shelves) with emissive factor 1,1,1.
  `windows_dark` has the day texture only. Pane UVs point into those two sheets (8 drawings, 4 by 2).
- house-01-bungalow (the white house), both sides and the back, same treatment the front got in u75m: stray sheets of
  old paint off each wall, the nine torn windows redrawn with frames (`trim`), four of them barred with straight iron
  bars (new material `bars`), the generator's bent bars and torn glass taken out, soot and stains round the windows
  patched with the wall's own clean paint. Plan: `house-01-bungalow.plan.json`.

## For the build

1. Use all eleven files as delivered. Nothing in the build should paint, tint or re-light window panes.
2. `windows_lit` now glows from an emission MAP, not a flat color. BindTextures already leaves a finished file's
   windows_lit alone and DayCycle scales `_EmissionColor`, which multiplies the map, so no code change should be
   needed. Confirm UrpMaterials carried `_EmissionMap` over and `_EMISSION` is on. The base emission color is now
   white (1,1,1); if panes read too hot at 22:00, lower the window glow level in DayCycle, do not tint the material.
3. Re-extract textures for the eleven models (tools/extract_building_textures.mjs, one name at a time). New for the
   bungalow: `house-01-bungalow__bars_basecolor.jpg`; its `__walls_*` maps changed. The window sheets are not
   extracted (the script skips window parts), they stay inside the GLB.
4. Update sizes and hashes in the SceneBuilder.cs comment block.
5. Shots to send back: the white house from all four sides at 14:00 and 22:00; pawn, barber, apt-01-stucco and
   apt-mixed-use fronts at 22:00 and 14:00.

## Known and left alone

- Which drawing a pane gets is by its size and place, not by what the building is. The barber's big windows got
  house blinds and a curtain, not a shop interior, because they start higher off the ground than the shopfront rule.
- The other houses' walls and doors were not redrawn. Only the white house has a hand plan so far.
- The white house keeps light weathering low on its left and back walls.
