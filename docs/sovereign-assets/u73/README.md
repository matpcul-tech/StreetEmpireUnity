# Sovereign Assets re-run for v73

Every model in the game went through Sovereign Asset (origin main f8ba6bb) again with the "Unity building or prop" preset (id unity-static) for buildings, props and cars, and the "Unity character" preset (id unity-character) for the eight characters. Driver: tools/sa-unity.mjs, which drives the real web app in a headless browser and saves the GLB, the report, the verdict JSON and the verdict image.

What exists in Sovereign Asset, named exactly:

- Presets: "Unity building or prop" and "Unity character" both exist.
- "Color standard" is a pass or fail check (floor 2%, ceiling 82%, vivid at most 90%). It only runs when the asset has a color texture, so the texture free Far and Low building LODs show "none" for it.
- "Texture detail" is a NOTE in texture pixels per model meter, not a pass or fail check. There is no minimum setting for it in the app.
- "Flatten walls" has Off, Light and Strong.

Building GLBs are about 1.9 model units wide and Unity scales them to 13 m on their long side, so the in game number is the Sovereign number times (long side / 13). Props, cars and characters are at real scale, so both numbers match.

Outlier fixes in this run:

- wood-fence was 702 px/m against a prop median near 500. It went back through Sovereign Asset with Largest texture 512 px and is now 351 px/m with 0% stretched.
- Over 1.6x the prop median were cut one step with the Largest texture option: hydrant and hydrant-aged 1538 to 769, street-lamp-1 994 to 497, bench 891 to 445, trash-can 859 to 430, tyre 948 to 474, security-light 900 to 450, street blades 1090 to 545.
- newspaper-box read 0 px/m because its planar label projection collapsed the sides and top to lines. tools/box_uv.mjs gives each face its own projection; it then read 833 px/m with 0% stretched (its texture is already 512, the smallest Largest texture option).
- Cutout props (decal-set, dry-hedge, dry-shrub, grass-clump, palm-short, palm-tall) first FAILED Color standard because fully transparent texels were black. tools/alpha_bleed.mjs bleeds color into them and keeps a small alpha floor (16 for alpha test, 6 for blend), because Sovereign Asset re-encodes through a premultiplied canvas that wipes the color of alpha 0 texels. All six then passed.
- traffic-signal and chainlink-panel (LOD1) FAILED Normals on the first pass and passed on a second pass with Fix normals on.

Still low (not fixed, they need new textures or new UVs): trees (tree-elm 44, tree-maple 48, tree-young 85), utility-pole 47, palms 106, covered-car 122, sedan 105, and every building at about 8 to 39 px/m in game. Buildings are the big gap: a 1024 Meshy atlas spread over a 13 m house.

house-01-bungalow: Flatten walls on Strong pressed only 74 triangles (0.31% at most). The torn left window is in the texture itself, so Flatten cannot fix it. It needs a Meshy regeneration, which was not done in this run (0 credits spent).

## Buildings (full)

| Asset | Verdict | Color standard | Texture detail (SA px/m) | In game px/m | Stretched % | Options |
|---|---|---|---|---|---|---|
| apt-01-stucco | READY | PASS | 80 | 12 | 10 | Flatten Light, window_maps first |
| apt-02-brick | READY | PASS | 74 | 8 | 16 | Flatten Strong, window_maps first |
| apt-mixed-use | READY | PASS | 179 | 18 | 20 | Flatten Light, window_maps first |
| apt-motel-style | READY | PASS | 127 | 19 | 12 | Flatten Light, window_maps first |
| civic-church | READY | PASS | 164 | 23 | 16 | Flatten Light, window_maps first |
| house-01-bungalow | READY | PASS | 115 | 17 | 10 | Flatten Strong, window_maps first |
| house-02-two-story | READY | PASS | 96 | 14 | 1 | Flatten Light, window_maps first |
| house-03-duplex | READY | PASS | 250 | 18 | 2 | Flatten Strong, window_maps first |
| house-boarded | READY | PASS | 134 | 20 | 15 | Flatten Light, window_maps first |
| house-rowhouse-pair | READY | PASS | 114 | 16 | 4 | Flatten Light, window_maps first |
| house-shotgun | READY | PASS | 146 | 21 | 19 | Flatten Light, window_maps first |
| house-wood-duplex | READY | PASS | 138 | 20 | 13 | Flatten Light, window_maps first |
| shop-auto-garage | READY | PASS | 98 | 14 | 29 | Flatten Strong, window_maps first |
| shop-gas-station | READY | PASS | 114 | 17 | 23 | Flatten Light, window_maps first |
| store-barber | READY | PASS | 103 | 15 | 15 | Flatten Strong, window_maps first |
| store-bodega | READY | PASS | 68 | 10 | 21 | Flatten Light, window_maps first |
| store-laundromat | READY | PASS | 125 | 18 | 8 | Flatten Strong, window_maps first |
| store-liquor-blank | READY | PASS | 50 | 39 | 0 | Flatten Light, window_maps first |
| store-pawn | READY | PASS | 122 | 18 | 12 | Flatten Light, window_maps first |
| store-takeout | READY | PASS | 123 | 16 | 2 | Flatten Light, window_maps first |

## Buildings Near LOD

| Asset | Verdict | Color standard | Texture detail (SA px/m) | In game px/m | Stretched % | Options |
|---|---|---|---|---|---|---|
| apt-01-stucco | READY | PASS | 40 | 6 | 10 |  |
| apt-02-brick | READY | PASS | 37 | 4 | 16 |  |
| apt-mixed-use | READY | PASS | 179 | 18 | 19 |  |
| apt-motel-style | READY | PASS | 64 | 9 | 13 |  |
| civic-church | READY | PASS | 163 | 23 | 16 |  |
| house-01-bungalow | READY | PASS | 57 | 8 | 10 |  |
| house-02-two-story | READY | PASS | 96 | 14 | 1 |  |
| house-03-duplex | READY | PASS | 250 | 18 | 2 |  |
| house-boarded | READY | PASS | 135 | 20 | 15 |  |
| house-rowhouse-pair | READY | PASS | 56 | 8 | 4 |  |
| house-shotgun | READY | PASS | 145 | 21 | 19 |  |
| house-wood-duplex | READY | PASS | 137 | 20 | 14 |  |
| shop-auto-garage | READY | PASS | 49 | 7 | 29 |  |
| shop-gas-station | READY | PASS | 57 | 8 | 23 |  |
| store-barber | READY | PASS | 103 | 15 | 15 |  |
| store-bodega | READY | PASS | 35 | 5 | 22 |  |
| store-laundromat | READY | PASS | 125 | 18 | 8 |  |
| store-pawn | READY | PASS | 122 | 18 | 12 |  |
| store-takeout | READY | PASS | 123 | 16 | 2 |  |

## Buildings Far LOD

| Asset | Verdict | Color standard | Texture detail (SA px/m) | In game px/m | Stretched % | Options |
|---|---|---|---|---|---|---|
| apt-01-stucco | READY | none | n/a | n/a | n/a |  |
| apt-02-brick | READY | none | n/a | n/a | n/a |  |
| apt-mixed-use | READY | none | n/a | n/a | n/a |  |
| apt-motel-style | READY | none | n/a | n/a | n/a |  |
| civic-church | READY | none | n/a | n/a | n/a |  |
| house-01-bungalow | READY | none | n/a | n/a | n/a |  |
| house-02-two-story | READY | none | n/a | n/a | n/a |  |
| house-03-duplex | READY | none | n/a | n/a | n/a |  |
| house-boarded | READY | none | n/a | n/a | n/a |  |
| house-rowhouse-pair | READY | none | n/a | n/a | n/a |  |
| house-shotgun | READY | none | n/a | n/a | n/a |  |
| house-wood-duplex | READY | none | n/a | n/a | n/a |  |
| shop-auto-garage | READY | none | n/a | n/a | n/a |  |
| shop-gas-station | READY | none | n/a | n/a | n/a |  |
| store-barber | READY | none | n/a | n/a | n/a |  |
| store-bodega | READY | none | n/a | n/a | n/a |  |
| store-laundromat | READY | none | n/a | n/a | n/a |  |
| store-pawn | READY | none | n/a | n/a | n/a |  |
| store-takeout | READY | none | n/a | n/a | n/a |  |

## Buildings Low LOD

| Asset | Verdict | Color standard | Texture detail (SA px/m) | In game px/m | Stretched % | Options |
|---|---|---|---|---|---|---|
| apt-01-stucco | READY | none | n/a | n/a | n/a |  |
| apt-02-brick | READY | none | n/a | n/a | n/a |  |
| apt-mixed-use | READY | none | n/a | n/a | n/a |  |
| apt-motel-style | READY | none | n/a | n/a | n/a |  |
| civic-church | READY | none | n/a | n/a | n/a |  |
| house-01-bungalow | READY | none | n/a | n/a | n/a |  |
| house-02-two-story | READY | none | n/a | n/a | n/a |  |
| house-03-duplex | READY | none | n/a | n/a | n/a |  |
| house-boarded | READY | none | n/a | n/a | n/a |  |
| house-rowhouse-pair | READY | none | n/a | n/a | n/a |  |
| house-shotgun | READY | none | n/a | n/a | n/a |  |
| house-wood-duplex | READY | none | n/a | n/a | n/a |  |
| shop-auto-garage | READY | none | n/a | n/a | n/a |  |
| shop-gas-station | READY | none | n/a | n/a | n/a |  |
| store-barber | READY | none | n/a | n/a | n/a |  |
| store-bodega | READY | none | n/a | n/a | n/a |  |
| store-laundromat | READY | none | n/a | n/a | n/a |  |
| store-pawn | READY | none | n/a | n/a | n/a |  |
| store-takeout | READY | none | n/a | n/a | n/a |  |

## Props

| Asset | Verdict | Color standard | Texture detail (SA px/m) | In game px/m | Stretched % | Options |
|---|---|---|---|---|---|---|
| aircon | READY | PASS | 553 | 553 | 0 |  |
| barrel | READY | PASS | 517 | 517 | 0 |  |
| bat | READY | PASS | 757 | 757 | 3 |  |
| bench | READY | PASS | 445 | 445 | 22 | Largest texture 1024 |
| bus-shelter | READY | none | n/a | n/a | n/a |  |
| bus-stop-sign | READY | PASS | 484 | 484 | 2 |  |
| bush | READY | PASS | 200 | 200 | 0 |  |
| cardboard-box | READY | PASS | 783 | 783 | 0 |  |
| chainlink-panel | READY | PASS | 201 | 201 | 0 |  |
| chainlink-post | READY | PASS | 406 | 406 | 0 |  |
| covered-car | READY | PASS | 122 | 122 | 8 |  |
| crate | READY | PASS | 604 | 604 | 0 |  |
| decal-set | READY | PASS | 160 | 160 | 0 | alpha_bleed first |
| dry-hedge | READY | PASS | 286 | 286 | 0 | alpha_bleed first |
| dry-shrub | READY | PASS | 240 | 240 | 0 | alpha_bleed first |
| dumpster | READY | PASS | 512 | 512 | 0 |  |
| grass-clump | READY | PASS | 824 | 824 | 0 | alpha_bleed first |
| hedge | READY | PASS | 202 | 202 | 0 |  |
| hydrant-aged | READY | PASS | 769 | 769 | 0 | Largest texture 1024 |
| hydrant | READY | PASS | 769 | 769 | 0 | Largest texture 1024 |
| mailbox | READY | PASS | 512 | 512 | 0 |  |
| manhole | READY | PASS | 684 | 684 | 0 |  |
| newspaper-box | READY | PASS | 833 | 833 | 0 | box_uv first |
| palm-short | READY | PASS | 106 | 106 | 0 | alpha_bleed first |
| palm-tall | READY | PASS | 106 | 106 | 0 | alpha_bleed first |
| road-barrier | READY | PASS | 466 | 466 | 0 |  |
| security-light | READY | PASS | 450 | 450 | 2 | Largest texture 512 |
| sign-post | READY | PASS | 497 | 497 | 0 |  |
| stop-sign | READY | PASS | 656 | 656 | 0 |  |
| street-blade-0 | READY | PASS | 545 | 545 | 9 | Largest texture 512 |
| street-blade-1 | READY | PASS | 545 | 545 | 9 | Largest texture 512 |
| street-blade-2 | READY | PASS | 545 | 545 | 9 | Largest texture 512 |
| street-blade-3 | READY | PASS | 545 | 545 | 9 | Largest texture 512 |
| street-blade-4 | READY | PASS | 545 | 545 | 9 | Largest texture 512 |
| street-blade-5 | READY | PASS | 545 | 545 | 9 | Largest texture 512 |
| street-blade-6 | READY | PASS | 545 | 545 | 9 | Largest texture 512 |
| street-blade-7 | READY | PASS | 545 | 545 | 9 | Largest texture 512 |
| street-lamp-1 | READY | PASS | 497 | 497 | 0 | Largest texture 1024 |
| traffic-signal | READY | PASS | 479 | 479 | 0 | second pass |
| trash-can-rust | READY | PASS | 432 | 432 | 0 |  |
| trash-can | READY | PASS | 430 | 430 | 0 | Largest texture 1024 |
| trashbag | READY | PASS | 832 | 832 | 0 |  |
| tree-elm | READY | PASS | 44 | 44 | 1 |  |
| tree-maple | READY | PASS | 48 | 48 | 0 |  |
| tree-young | READY | PASS | 85 | 85 | 1 |  |
| tyre | READY | PASS | 474 | 474 | 25 | Largest texture 512 |
| utility-box-1 | READY | PASS | 517 | 517 | 0 |  |
| utility-box-2 | READY | PASS | 412 | 412 | 0 |  |
| utility-pole | READY | PASS | 47 | 47 | 0 |  |
| wood-fence | READY | PASS | 351 | 351 | 0 | Largest texture 512 |

## Props LOD1

| Asset | Verdict | Color standard | Texture detail (SA px/m) | In game px/m | Stretched % | Options |
|---|---|---|---|---|---|---|
| aircon | READY | PASS | 285 | 285 | 0 |  |
| barrel | READY | PASS | 259 | 259 | 0 |  |
| cardboard-box | READY | PASS | 392 | 392 | 0 |  |
| chainlink-panel | READY | PASS | 100 | 100 | 0 | second pass |
| crate | READY | PASS | 302 | 302 | 0 |  |
| hydrant-aged | READY | PASS | 389 | 389 | 1 |  |
| hydrant | READY | PASS | 389 | 389 | 1 |  |
| manhole | READY | PASS | 342 | 342 | 0 |  |
| road-barrier | READY | PASS | 233 | 233 | 0 |  |
| security-light | READY | PASS | 460 | 460 | 3 |  |
| street-lamp-1 | READY | PASS | 249 | 249 | 0 |  |
| trash-can-rust | READY | PASS | 217 | 217 | 0 |  |
| trash-can | READY | PASS | 216 | 216 | 0 |  |
| trashbag | READY | PASS | 419 | 419 | 0 |  |
| tyre | READY | PASS | 446 | 446 | 27 |  |
| utility-box-1 | READY | PASS | 259 | 259 | 0 |  |
| utility-box-2 | READY | PASS | 206 | 206 | 0 |  |

## Cars

| Asset | Verdict | Color standard | Texture detail (SA px/m) | In game px/m | Stretched % | Options |
|---|---|---|---|---|---|---|
| police | READY | PASS | 512 | 512 | 4 |  |
| sedan | READY | PASS | 105 | 105 | 9 |  |

## Cars LOD1

| Asset | Verdict | Color standard | Texture detail (SA px/m) | In game px/m | Stretched % | Options |
|---|---|---|---|---|---|---|
| police | READY | PASS | 512 | 512 | 5 |  |
| sedan | READY | PASS | 105 | 105 | 8 |  |

## Characters

| Asset | Verdict | Color standard | Texture detail (SA px/m) | In game px/m | Stretched % | Options |
|---|---|---|---|---|---|---|
| cop-m-1 (cop-m-1-rigged-1024) | READY | PASS | 400 | 400 | 0 |  |
| cop-m-2 | READY | PASS | 400 | 400 | 0 |  |
| ped-f-1 (locals-f-1-rigged-1024) | READY | PASS | 499 | 499 | 0 |  |
| ped-f-2 (locals-f-2-rigged-1024) | READY | PASS | 473 | 473 | 0 |  |
| ped-m-1 (locals-m-1-rigged-1024) | READY | PASS | 475 | 475 | 0 |  |
| ped-m-2 (locals-m-2-rigged-1024) | READY | PASS | 491 | 491 | 0 |  |
| player (player-v2-rigged-1024) | READY | PASS | 477 | 477 | 0 |  |
| unc (unc-v2-rigged-1024) | READY | PASS | 345 | 345 | 0 |  |

