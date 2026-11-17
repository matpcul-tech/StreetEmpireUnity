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
