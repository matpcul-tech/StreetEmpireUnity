# NPC v4 in the game (Nov 22, 2026)

Source: release `meshy-npc-v4-2026-11-22`, ten GLBs, all verified against MANIFEST.txt (SHA-256).

| File | Use |
|---|---|
| reds-m-3, reds-f-2 | Red Kings guards and raiders |
| blues-m-3, blues-f-2 | Blue Hand guards and raiders |
| golds-m-3, golds-f-2 | Gold Line guards and raiders |
| player-f-1, player-m-2, player-m-3, player-f-4 | Player choices. In the project, not yet selectable in the game. |

## What was done

- Converted from the delivered bytes with the repo's tools (tools/glb_to_fbx.py, tools/tpose_fbx.py,
  tools/extract_textures.mjs): T-posed FBX plus 1024 base color and normal JPEGs. No bone was missing on any of the
  ten. About 14,900 triangles each. converted-tpose.jpg shows all ten after conversion.
- Nothing in the files was changed. Skin tones were fine as delivered, so the v3 skin grade was not run.
- Crew bodies are under Assets/Art/Models/Characters/Crews, player choices under Characters/Player. Plain git files,
  not LFS (see .gitattributes).

## In the game

- TurfWar has three bodies for each rival crew (man, woman, man with a bat). A guard or raider from the Red Kings,
  Blue Hand or Gold Line is copied from his own crew's bodies. Your guards, the Locals and the opening muggers still
  use the plain pedestrian bodies.
- Heights: every file is 1.80 m. In the game: reds 1.80 and 1.68, blues 1.76 and 1.64, golds 1.82 and 1.72.
- The flank fix the pedestrians get (GameBuilder.ArmFix) is not applied. Measured on the files, the crews' torso
  sides follow the arm bones about as much as the cops and the player do (8 to 37 percent of flank vertices, against
  49 on ped-m-1), and those two are not fixed either.

## Not checked from here

- How they look moving in a running game: the long Gold Line coats in the walk, and reds-m-3's sides with his arms
  down (he is the highest of the six on the flank measure).
- Gold Line reads as brown, not gold, and the Blue Hand vests read as work vests. That is in the art, not something
  the conversion changes.
