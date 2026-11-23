# Cars v2 in the game (Nov 22, 2026)

Source: release `meshy-cars-v2-2026-11-22`, nine cars, the 1024 px files, all verified against MANIFEST.txt
(SHA-256): sedan-clean, sedan, police, coupe, lowrider, pickup, suv, taxi, van. renders.jpg shows each from the
release bytes, front and rear.

As delivered: 13,500 to 19,900 triangles, real meters (4.50 to 5.35 m long), body, wheel and glass materials, four
wheel nodes (wheel_fl, wheel_fr, wheel_rl, wheel_rr) pivoting at the hub.

## What tools/car_v2.mjs changes

| Part | Change | Why |
|---|---|---|
| body | Metal factor 1 to 0, roughness 1 to 0.5, metal-roughness map dropped | The game's material converter does not read that map; a metal factor of 1 without it turns the whole car chrome (the old cruiser had this). The body is an even semi-gloss paint now, bumpers included. |
| glass | Roughness 0.08 to 0.35, color pure black to a very dark blue-gray | At 0.08 the glass mirrors the sun and flashes white from some angles (seen in the review sheet). |
| maps | PNG to JPEG | About a quarter of the file size. |
| far version | Welded and cut to about 45 percent of the triangles, same maps and UVs | Drawn past 15 m. |

Mesh, UVs, wheel pivots and size are as delivered.

## In the game

- The car you drive is sedan-clean. The police cruisers that come for you are the v2 police car.
- Parked cars on the curbs and in the lots are a mix of all nine, a cruiser now and then.
- The two old cars (Models/Cars/sedan.glb, police.glb) are no longer used by the scene.

## Not done

- Paint colors. The body is one painted texture with no paint mask, so a recolor needs a mask made from the paint
  color first.
- POLICE and TAXI lettering.
- The muscle slot in vehicles.json has no car of its own (the coupe is the nearest). No motorcycles in this release.
- The known flaws listed in the release notes (sedan-clean has no B-pillar in the side glass, coupe rear hatch glass
  is texture only, lowrider liner band behind the tires, suv pillars are part of the glass) are as delivered.

## Not checked from here

How they look and drive in a running game: wheel spin direction, how the semi-gloss paint reads at dusk, and whether
the parked mix costs frame rate on a phone.

# Motorcycles v2 (Nov 22, 2026)

Source: release `meshy-bikes-v2-2026-11-22`, the 1024 px files, verified against MANIFEST.txt: cruiser, dirt-bike,
sport-bike. 10,000 to 11,700 triangles, 2.10 to 2.40 m long, body and wheel materials, a steer node (fork, bars,
front fender) on the steering-head axis with wheel_f under it, wheel_r separate. bikes.jpg shows them from the
prepared files.

Prepared by the same tools/car_v2.mjs (body to an even semi-gloss, JPEG maps, far version). The far versions only
drop to 72 to 82 percent of the triangles: the spokes and frame tubes do not simplify further at the same error.

In the game: parked at the curb on some of the sides that have no parked cars, leaned 9 degrees as if on a stand.

Not done: riding them. That needs a seated rider pose, lean in the turns and the steer node turning with the bars;
the driving code today only knows cars.

# v75t on the phone: the cars came out in camouflage (fixed Nov 22, 2026)

Seen in the v75t test build: every v2 car body was a patchwork of lighter and darker triangles. Color, wheels and
glass were right; the shading was wrong.

Cause, read out of the v75t APK:
- Android builds use the DXT5nm-style normal encoding (ProjectSettings m_BuildTargetNormalMapEncoding 1). A normal
  map imported from a file is stored that way. The car normal maps were images inside the GLBs, stored as plain
  RGB, so the shader took X from an alpha channel that is not there and every normal leaned hard along its tangent.
- These bodies are unwrapped as a mosaic of small charts, each turned its own way. A lean along the tangent lights
  each chart differently. The old kit sedan has a tidy unwrap and its maps were files, so it never showed.
- The mesh, its UVs and its tangents in the build are right (checked against the UVs), and so are the color maps.

Fix (tools/car_v2.mjs, VehicleBuilder.BindV2):
- No normal map on the cars. The mesh normals are smooth; renders with and without the map look the same.
- The color maps are JPEG files under Art/Textures/Cars/V2 and the GLBs carry no images. They import as ordinary
  compressed textures, and the near and far car share one copy (v75t carried two uncompressed 1024 copies of every
  body map plus two normal maps, about 16 MB of memory a car).

Not checked from here: the fix on a phone.

# v75u: lettering, glass, and the SUV roof (Nov 22, 2026)

From the phone, on build 14652fa: the cruiser and taxi had no lettering, every window was a flat black panel, and
the SUV had a black square on its roof. All three are in tools/car_v2.mjs and VehicleBuilder.BindV2.
lettering-glass-roof.jpg shows the cruiser, taxi and SUV from both sides, above and front (the cruiser's lettering
was narrowed to its white doors after that sheet was made).

- Lettering: "PORT CALDER / POLICE" on the cruiser's white doors, "NIGHTOWL CAB CO. / TAXI / 555-0144" on the
  taxi's. Both names are invented. It sits on a thin panel copied from the body's own side faces, 1 cm out, with
  its own UVs laid flat, so it follows the doors and reads the right way round on both sides. The words are a
  cut-out PNG (<kind>_decal.png). It cannot be painted into the body map, which is a mosaic of small charts.
- Glass: one shared map (glass.png), lighter toward the top with two soft streaks, UVs laid by height and length,
  and a little of it glowing so windows still read at night.
- SUV roof: its map painted the roof near black. The dark texels of the roof's upward faces take the door paint.

Not done: the taxi's roof sign is still blank, every cruiser carries the same lettering (no unit numbers), and no
paint colors yet.

Not checked from here: any of this on a phone.
