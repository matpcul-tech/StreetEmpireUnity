# Matthew's checklist: Unity vs the original web game

Reference: matpcul-tech/StreetEmpire (read only) src/main.js, src/state.js, src/city.js, src/style.css, index.html,
and the live game (shots in /workspace/shots/web-original). Audit Nov 17, 2026 on v67; status updated after the
work below. Order of work was by visual impact.

| # | Item | Web game | Unity v67 (audit) | Status now |
|---|---|---|---|---|
| 1 | Player and follow camera | Camera 5.2 m behind, pivot 1.7 m, aimed 1.2 m past the player and 0.6 m right (over the shoulder), pitch starts at 0.3 rad (17 deg), range -0.1 to 1.1 rad, 0.0026 rad per mouse pixel, eased follow, pulled in when a wall is behind, FOV 62 | Cinemachine orbit, 5.2 m, FOV 58, pitch 14 deg in -8 to 60, centered, no wall pull in | Done (63f1e56): FOV 62, pivot 1.7, pitch 17 in -6 to 63, web mouse speed, right shoulder on foot, eased follow, pulled in by building walls only. Player model unchanged (the 720 credit character batch is on hold) |
| 2 | Sunset sky, fog, grade | Golden hour, warm haze, ACES plus grade | Done in v67, but a horizon line and green outer ground | Done (3d4f9cc): sky below the horizon is the fog color, outer ground dry grass |
| 3 | NPCs and the mugger mission | Mugger 2.5 s in, 9 m ahead, grab at 2.3 m and shove; two friends (50 hp fists, 56 hp bat) when he is hurt, grabs or after 3.2 s; "The street goes quiet. Unc wants a word."; Red Kings scout drive by; onboarding 1/3, 2/3, 3/3 | No mugger opening, no onboarding | Done (2a54bb9 and later): MuggerOpening and Brawler with the web numbers (reach, tells, lunge, damage, strike cap 2, ring at 6.6 m), bat model (Sovereign Asset READY) in the bat friend's hand, web messages, onboarding box 1/3 and 2/3. Left: the scout drive by; 3/3 (turf capture) is not in Unity; muggers reuse pedestrian models |
| 4 | Minimap and heat HUD | Round radar top left, turns with the camera; top right clock, weapon, cash #f4e6c4, HEAT five slanted bars, red health bar, Rep and Turf; message strip | Green cash, five diamonds, no radar | Done: radar (blocks by crew, capture rings, dashed streets, Unc dot and letter, job dot at the rim, people and flashing cops, white arrow, N), the top right column in web colors and sizes, onboarding box, message strip, web keys (F or click hits, E uses). The clock runs like the web clock while the light stays at golden hour |
| 5 | Fog hides the world edge on Ultra and High | as 2 | Not checked | Verified on both tiers (tour69 and tour69high, edge street and overview views) |
| 6 | Decals | Grime, cracks, trash | None | Done (21abb6b): cracks, oil, litter on streets and lots, graffiti on real walls; Sovereign Asset READY |
| 7 | Palms, no blob foliage | Palms, dry yards | Hedges and bushes were blobs | Done (21abb6b): dry shrub and dry hedge models, READY |
| 8 | Houses for the Eastside | Stucco, tile roofs, bars | Clapboard and brick houses read wrong | Done by replacement (3d4f9cc): only the three stucco, tile roof, barred window models with five stucco washes; house-03 once in seven for the frame budget. Not a Meshy reskin (no Meshy access on the box). store-pawn left as is (Matthew deciding) |
