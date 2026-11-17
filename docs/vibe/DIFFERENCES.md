# Unity Eastside vs the original web Street Empire: what sets the vibe (Nov 17, 2026)

Original: matpcul-tech/StreetEmpire v0.29 (read only), live at https://matpcul-tech.github.io/StreetEmpire/,
captured in headless Chrome (?high&pin&nointro&novideo&new). Unity: v65 tour. Sheet: compare-before.jpg.

| | Original web | Unity v65 |
|---|---|---|
| Time of day | Golden hour: a new game starts at 17:48 with the sun about 3 degrees up, low along the street (8 minute day and night cycle) | Fixed midday, sun 48 degrees up |
| Light | Warm sun (fff0dd to ffb27a), a strong lavender sky fill (hemisphere 9a94b4 over 6a5a58) that outweighs the sun, soft warm shadows | Near white sun at 1.7, cool blue grey trilight ambient, hard bright sunlight |
| Sky | Hazy cream and peach near the horizon, warm smog | Saturated blue procedural sky |
| Fog | Warm mauve peach (b48a66 toward 8a7088), from 62 m to 270 m | Cool blue grey, 140 m to 420 m |
| Grade | ACES, then a LUT: strong S curve, saturation 0.94, golden highlights, deep brown lifted shadows; bloom 0.4 over 0.9; heavy vignette (0.45); faint color split at the edges | ACES, mild contrast 12, saturation +8, vignette 0.18, bloom 0.35 |
| Overall brightness | Moody, about a third of the Unity light level before tonemapping | Bright and clean |
| Trees | Eastside street trees are tall palms: 9 m tapered trunk (7a6650), a crown of drooping olive fronds, set along the curbs and seen as silhouettes against the sky | Round leafy maples and elms in the lots and yards (the web game uses leafy trees only downtown) |
| Poles and wires | Wooden telephone poles every 13 m on every curb, 9 m tall with crossbars, sagging wires | Fewer poles |
| Lights | Street lamps and shop neon (pink, purple) glow at dusk | Lamps unlit, no neon |
| Road markings | One dashed dim yellow center line (b59a3a), dull grey crosswalks (9a9a92) | Double solid yellow, bright white zebra crosswalks |
| Yards | Dark, dry olive grass (56562f) | Lighter grass |
| Setting | West Coast: palms, stucco, tile roofs | Chicago style street names (E 31ST ST, KING AVE), Midwestern trees, chain link |
