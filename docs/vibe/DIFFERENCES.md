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

## What changed in Unity (v67, run 67, commits 585bd95, d4b5139, 29fd99f)

Sheet: compare-after.jpg (original web, Unity v65, Unity v67 in the same kinds of views). Tour: tour-v67.jpg.

- Time of day: a fixed golden hour (the sun 9 degrees up, along the web game's sun direction). The web game has a full 8 minute day and night cycle; Unity has none yet.
- Light: a warm sun (1, 0.77, 0.58) at 1.05, a lavender sky fill and warm ground fill (trilight) that carry most of the light, as in the web game. Shadow bias raised a little for the grazing sun.
- Sky: a generated golden hour sky cubemap (half float): cream and peach at the horizon, a warm glow round the sun, dusty cream overhead. The same sky, smaller, is the reflection map, at 0.35 strength.
- Fog: warm mauve peach, linear 62 m to 270 m (the web High numbers).
- Grade: ACES, bloom 0.45 over 0.9 with a warm tint, contrast 20, saturation -6, white balance +10, split toning (golden highlights, brown shadows), lift and gain warm, vignette 0.36 in dark brown.
- Trees: all the leafy trees replaced by palms made after the web palm (own work, tools/model_palms.py, 124 triangles each, Sovereign Asset Game ready, READY), plus curb palms on every block side like the web Eastside.
- Street lamps glow warm.
- Center lines in the web yellow (b59a3a), crosswalks and stall lines a worn grey, yards a dry olive.

## Not changed (flagged)

- Center lines are still double solid, not the web's single dashed line.
- No shop neon signs yet.
- Street names are still Chicago style.
- The overview view (tour 7) is now mostly hidden by the 270 m fog.
- Telephone poles are still only on the east curbs (the web game has them on every curb, every 13 m).

## Draw calls and triangles per tour view, v65 to v67

| View | v65 draws | v67 draws | v65 tris | v67 tris |
|---|---|---|---|---|
| 1 | 430 | 422 | 1,403,070 | 1,313,102 |
| 2 | 471 | 461 | 1,441,373 | 1,370,694 |
| 3 | 429 | 422 | 1,244,122 | 1,157,040 |
| 4 | 283 | 273 | 932,758 | 833,862 |
| 5 | 462 | 433 | 1,756,652 | 1,538,171 |
| 6 | 362 | 362 | 1,148,982 | 1,066,180 |
| 7 | 322 | 318 | 856,027 | 823,451 |
| 8 | 164 | 146 | 318,504 | 264,012 |

Phone frame rate is unverified until Matthew tests on a phone.
