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
