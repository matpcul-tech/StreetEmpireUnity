# Art credits

| File | Source |
| --- | --- |
| Models/Buildings/*.glb | Original Street Empire town buildings made for this project (matpcul-tech/StreetEmpire public/models/kit/buildings). Not third-party downloads. |
| Models/Cars/sedan.glb | Street Empire kit sedan (public/models/kit/cars/sedan-v11.glb). |

Converted for Unity glTFast with tools/convert_glb.mjs: meshopt decoded, dequantized, WebP textures re-encoded as JPEG (PNG where there is alpha).
| Models/Characters/Player/player.fbx and textures | Street Empire player v2 character (Meshy generation made earlier for this project, Mixamo rig). Converted from GLB to FBX in Blender (tools/glb_to_fbx.py) and set to a T-pose rest (tools/tpose_fbx.py) so Unity Humanoid retargets cleanly. |
| Animations/ual-clips.fbx | Universal Animation Library by Quaternius, CC0 (public domain). Idle, walk, jog, sprint and jump clips. |
