# Art credits

| File | Source |
| --- | --- |
| Models/Buildings/*.glb | Original Street Empire town buildings made for this project (matpcul-tech/StreetEmpire public/models/kit/buildings). Not third-party downloads. M5: apt-01, apt-02, house-02 and house-03 replaced with the fixed versions from the Eastside buildings v2 pass (existing-fixed), same names. house-01 stays the original (the fixed one had faceted normals) with metallic set to 0. Far LODs made with tools/decimate_far.py, textures extracted with tools/extract_building_textures.mjs. |
| Models/Buildings/ (M5 new: store-bodega, store-laundromat, store-barber, store-pawn, store-takeout, shop-auto-garage, shop-gas-station, civic-church, house-rowhouse-pair, house-wood-duplex, house-shotgun, house-boarded, apt-mixed-use, apt-motel-style) | Eastside buildings v2, Meshy generations made for this project (about 58k triangles, 1024 textures, metallic 0, front +Z). Not third-party downloads. |
| Models/Cars/sedan.glb | Street Empire kit sedan (public/models/kit/cars/sedan-v11.glb). |

Converted for Unity glTFast with tools/convert_glb.mjs: meshopt decoded, dequantized, WebP textures re-encoded as JPEG (PNG where there is alpha).
| Models/Characters/Player/player.fbx and textures | Street Empire player v2 character (Meshy generation made earlier for this project, Mixamo rig). Converted from GLB to FBX in Blender (tools/glb_to_fbx.py) and set to a T-pose rest (tools/tpose_fbx.py) so Unity Humanoid retargets cleanly. |
| Animations/ual-clips.fbx | Universal Animation Library by Quaternius, CC0 (public domain). Idle, walk, jog, sprint and jump clips, plus (M5) Punch_Jab, Punch_Cross, Hit_Chest and Death01 for punches, flinches and knockdowns. |
| Models/Characters/Unc/unc.fbx and textures | Street Empire Unc v2 (Meshy generation made earlier for this project, character-v2-2026-11-13 sovereign run, Mixamo rig). Same Blender conversion as the player (tools/convert_character.sh). |
| Models/Characters/Cops/cop-m-1.fbx, cop-m-2.fbx and textures | Street Empire police roster cop-m-1 (1.82 m) and cop-m-2 (1.76 m, gray hair), Meshy generations made earlier for this project. Same conversion. |
| Models/Characters/Peds/ped-*.fbx and textures | Street Empire Locals models reused as pedestrians (pedestrian-reuse: ped-m-1, ped-m-2, ped-f-1, ped-f-2), Meshy generations made earlier for this project. Same conversion. |
| Models/Cars/police.glb | Street Empire kit police cruiser (public/models/kit/cars/police-v7.glb, made from the Meshy police-01 model). Converted with tools/convert_glb.mjs. |
| Models/Cars/sedan_paint_6a2420.jpg | The sedan atlas repainted deep red by tools/paint_car.mjs, the same rule kit.js uses for car paint. |
| Textures/Cars/police_* | Extracted from Models/Cars/police.glb by tools/extract_car_textures.mjs (metallic-roughness rewritten for URP) so Unity compresses them. |

## Eastside build-out (November 2026)

Every prop below was finished, exported and checked in Sovereign Asset (/asset.html in Sovereign-draft, "game" preset, keep format, ground and center, Fix normals) before it came into Unity. The finished files are Models/Props/*.glb; the Sovereign Asset reports and verdicts are in docs/sovereign-assets/props. Textures were pulled out of the finished files by tools/extract_prop_textures.mjs (Textures/Props, with manifest.json) so Unity compresses them.

| File | Source |
| --- | --- |
| Models/Props/hydrant.glb, hydrant-aged.glb | Poly Haven "Fire Hydrant" (fire_hydrant), CC0 1.0 (public domain). polyhaven.com/a/fire_hydrant |
| Models/Props/bench.glb | Poly Haven "Painted Wooden Bench" (painted_wooden_bench), CC0 1.0 |
| Models/Props/trash-can.glb, trash-can-rust.glb | Poly Haven "Metal Trash Can" (metal_trash_can, plain and rust variants), CC0 1.0 |
| Models/Props/street-lamp-1.glb | Poly Haven "Street Lamp 01" (street_lamp_01), CC0 1.0 |
| Models/Props/utility-box-1.glb, utility-box-2.glb | Poly Haven "Utility Box 01" and "Utility Box 02", CC0 1.0 |
| Models/Props/manhole.glb | Poly Haven "Water Manhole Cover" (water_manhole_cover), CC0 1.0 |
| Models/Props/trashbag.glb | Poly Haven "Trash Bag" (trashbag), CC0 1.0 |
| Models/Props/barrel.glb | Poly Haven "Barrel 03" (barrel_03), CC0 1.0 |
| Models/Props/tyre.glb | Poly Haven "Old Tyre" (old_tyre), CC0 1.0 |
| Models/Props/road-barrier.glb | Poly Haven "Concrete Road Barrier" (concrete_road_barrier), CC0 1.0 |
| Models/Props/security-light.glb | Poly Haven "Security Light" (security_light), CC0 1.0 |
| Models/Props/cardboard-box.glb | Poly Haven "Cardboard Box 01" (cardboard_box_01), CC0 1.0 |
| Models/Props/crate.glb | Poly Haven "Plastic Crate 02" (plastic_crate_02), CC0 1.0 |
| Models/Props/chainlink-panel.glb, chainlink-post.glb | Poly Haven "Modular Chainlink Fence" (modular_chainlink_fence), CC0 1.0 |
| Models/Props/aircon.glb | Poly Haven "Exterior Aircon Unit" (exterior_aircon_unit), CC0 1.0 |
| Models/Props/covered-car.glb | Poly Haven "Covered Car" (covered_car), CC0 1.0 (finished, not placed yet) |
| Models/Props/stop-sign, sign-post, street-blade-0..7, traffic-signal, mailbox, newspaper-box, dumpster, utility-pole, wood-fence, bus-stop-sign, bus-shelter | Modeled for this project in Blender (own work, no third-party source). Sign faces and grime textures drawn for this project with PIL. The street names are made up for the game. |
| Models/Props/tree-maple, tree-elm, tree-young, hedge, bush | Modeled for this project in Blender (own work), leaf and bark textures generated for this project. |
| Models/Props/palm-tall, palm-short | Modeled for this project (own work, tools/model_palms.py) after the original web game's procedural Eastside palm: tapered trunk, nine drooping alpha frond planes. Frond and bark textures drawn for this project with PIL in the web palette. Sovereign Asset Game ready, both READY. |
| Textures/Ground/worn_asphalt, concrete_pavement, sparse_grass, leafy_grass, park_dirt, cracked_concrete | Poly Haven textures of the same names, CC0 1.0 (1k diffuse and OpenGL normal maps). |

Poly Haven assets are CC0: no attribution is required; it is given here anyway.
