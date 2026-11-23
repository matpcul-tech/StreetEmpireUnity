# Vehicles wave C in the game (Nov 23, 2026)

Source: release `meshy-vehicles-wave-c-2026-11-23`, seventeen GLBs, all verified against MANIFEST.txt (SHA-256).
check-1.jpg and check-2.jpg show every one from the release bytes, front and rear (a flat, unlit render).

As delivered: 14,000 to 28,500 triangles, real meters, nose along +Z, wheel nodes at the hubs (six on the trucks),
and named materials: paint, trim, marking and lights share one 1024 color map; well, glass, wheel_rim and
wheel_tire are flat colors. The tractor and the trailer each carry a hitch node.

## Sixteen in, one held

In: hatchback, minivan, luxury-sedan, limo, tow-truck, ambulance, ice-cream-truck, armored-truck, swat-van,
school-bus, city-bus, fire-engine, garbage-truck, dump-truck, semi-tractor, semi-trailer.

Held: box-truck. Its cab still has black glass spilled over the windshield surround and the door.

Seen and let through: the ambulance's red stripe is ragged at the nose; the ice cream truck and the armored truck
have dark scuffs on the body; the luxury sedan's tail is rough close up.

## What tools/car_wavec.mjs does

The color map goes out as a JPEG file (Textures/Cars/V2/<kind>_body.jpg) and the GLB carries no image; the glass
gets the shared glass UVs; KHR_materials_specular is dropped; a far version is made. Mesh, size, wheel pivots and
material names are as delivered. The far versions of the trucks are still 13,000 to 21,000 triangles.

## In the game

- hatchback, minivan and luxury-sedan are in the curb mix with the other cars.
- The thirteen long ones (6.5 to 16 m) do not fit a car's spot. One is parked along some of the sides that had no
  parked cars or motorcycles, centered on the side.
- All parked. None can be driven, towed or hitched yet, and none has a paint color beyond its own.

## Not checked from here

Any of this in a running game: how a 12 m bus sits at the curb, and what the extra triangles cost on a phone.
