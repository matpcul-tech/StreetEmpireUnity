# u75q: storefront sign fixes, takeout contrast, night windows on two stores

Follow-up on v75p (5ae5578). No Meshy credits, no decimation, the six v5 models stay out, no tire day swap.

## Signs

- Check cashing: the board is hung again, a little taller and further off the yellow band (8.20 x 0.72 m, 8 cm out), both faces drawn, so an import X fold cannot hide the letters. Copy is still CHECK CASHING.
- Takeout: larger board (6.05 x 0.88 m) and a higher-contrast paint (near-black maroon, brighter cream letters). Copy is still CHICKEN · TAKEOUT.
- Gas: GAS · FOOD now hangs on the canopy's street-facing fascia (5.60 x 0.78 m at 4.68 m), not the recessed kiosk band under the roof. The price totem stands on the side away from the kiosk door (glTFast folds X, so that is -X in the holder), a little in front of the canopy. Night bloom on REG / PLUS / SUPER is reduced (tighter emission map, 1.15 boost instead of 2.2).
- Day glow: DayCycle sign floor is 0.10 (was 0.35). Letter emission boost is 1.55 (was 2.2), so 14:00 is lit but subtle and 22:00 stays bright.

## Palms and camera

- Curb palms that stood in front of the takeout and check cashing boards are slid along the curb to the side of that lot, before the instancer records their matrices.
- ShotDirector shot=v75q (shot=v75p still works) steps the camera sideways, and a little further back if needed, so a pole, palm or lamp is not on the line of sight. Signs are not hidden to clear the view.

## Windows at 22:00 (item 5)

Liquor, laundromat and shop-auto-garage were run through the normal Sovereign Assets Unity building finish, no triangle target, `walls: "off"` (the preset's `walls: "auto"` is what swapped scanned wall paint on the v5 models). Verdicts are in this folder. None of the finished GLBs were copied into the game.

- store-liquor-blank: READY, 1,572 triangles unchanged. findWindows found 0 panes (the kit glass is Atlas_OpaqueReflectiveGlass, flush in the frame, not a sunken rectangle). No windows_lit was created. Item 5 is stopped for liquor: the in-game mesh is unchanged, and those fronts stay dark at 22:00. The panes were not assistant-tagged.
- store-laundromat: READY, 58,000 to 58,020 triangles (no cut). Wall color texture bytes were identical to the source. findWindows found 0 new panes, so no Barber-style windows_day / windows_night maps were written. The file already has windows_lit (1,039 tris, storefront and side panes, not doors) with emissive factor 1, 0.74, 0.45 and a shared atlas on the glass. The original mesh is kept so flatten cannot change the wall faces, and the store stays out of the Finished set so its WallPaint (the blue-gray wash) is not dropped.
- shop-auto-garage: NOT READY (41 empty triangles remain), 38,242 to 38,399 triangles. Wall color texture bytes were identical. findWindows found 0 new panes. Existing windows_lit (716 tris) is the right-hand front window plus back and side panes, not the closed bay doors. The failed finish was not copied in.

Laundromat and auto now glow at night from that existing windows_lit factor: BindTextures keeps the panes as clean dark glass by day (no atlas, no triangle lines) and leaves the emissive factor on so DayCycle can ramp them. This is a flat warm pane, not the room-behind-glass maps the barber has.

## What this is not

Not game-ready. Raw store windows other than the two above stay dark. Auto bays stay closed. The mid-dot still reads like a hyphen, same as liquor and pawn. HUD and joystick still sit in the shots.
