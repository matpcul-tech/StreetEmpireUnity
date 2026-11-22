# Wave A characters in the game (Nov 22, 2026)

Source: release `meshy-chars-wave-a-2026-11-22`, ten GLBs, all verified against MANIFEST.txt (SHA-256). Converted
as delivered with the repo's tools (T-posed FBX, 1024 base color and normal JPEGs); no bone missing on any of the
ten. converted-tpose.jpg shows all ten after conversion. Nothing in the files was changed.

| File | Use |
|---|---|
| ped-m-3, ped-m-4, ped-m-5, ped-f-3, ped-f-4, ped-f-5 | Six more townspeople (ten in all) |
| boss-redkings, boss-bluehand, boss-goldline | The crew bosses |
| contact-fence | In the project, no part in the game yet |

## In the game

- TownLife copies its townspeople from all ten pedestrian bodies in turn. The four who walk the home block and the
  opening muggers are still the v3 four.
- A rival crew's boss stands guard on that crew's last block, in place of the first guard: 150 health against a
  guard's 60, no bat. At the start each rival crew holds one block, so each boss is on his corner from the start.
  If the crew takes a second block he is not out; cut them back to one and he is.
- Heights (the files all come at 1.80 m): pedestrians 1.60 to 1.80, bosses 1.84, 1.72, 1.78, fence 1.66.
- The flank fix (GameBuilder.ArmFix) stays on the v3 four only. On the wave A files 13 to 25 percent of flank
  vertices follow the arm bones, against 49 on ped-m-1.

## Not checked from here

How they move in a running game. The release's own sheets show two walk frames each; the long coats on the Red
Kings and Blue Hand bosses are the ones to watch in a fight.
