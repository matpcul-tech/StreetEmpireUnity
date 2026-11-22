# The town: 25 blocks

The town is the corner 5 x 5 of the city grid (`SceneBuilder.Town`, `SceneBuilder.TownBlocks`). It is built from the
grid, the kits and the block list alone, with no random placement, so it is the same town on every machine. That
is what a shared multiplayer world needs: every player's copy has the same building on the same lot.

## What stands where

- **The original nine blocks** (the Eastside 3 x 3) are untouched: every building is on the lot it had.
- **Six more Eastside blocks** along the two outer edges carry on the Eastside kit: stucco houses, tile roofs, the
  same stores in turn. No second gas station, tire shop or church.
- **Nine Midtown blocks** toward the middle (grid x + z from 5 to 7) take the older models that were left out of the
  Eastside for reading brick and clapboard: house-rowhouse-pair, house-wood-duplex, house-shotgun, house-boarded,
  apt-motel-style, store-bodega, with apt-02-brick and the other apartments. Midtown is mostly apartments, so the
  town gets denser and older toward the middle.
- **One Downtown block** (4, 4) should be towers. There are no tower models in this repo yet, so it takes four
  apartments. The same goes for any Harbor shed lot later.

Counts: 100 buildings. Eastside 39 houses, 15 stores, 6 apartments. Midtown and the Downtown block 25 apartments,
9 stores, 6 houses.

## What repeats, and what new art fixes

- Apartments: 31 lots from 5 models. This is the thinnest part.
- Stores: 24 lots from 10 models.
- Eastside houses: 39 lots from 8 models, with a wash index that keeps a block from repeating one.
- Street names: six streets each way, but only four name blades each way exist, so the fifth and sixth posts show
  the first and second names again (`PropBuilder.Blades`). Two new blades each way fix it.
- New models go into `SceneBuilder.Kit` (Eastside) or `SceneBuilder.MidKit` (Midtown) and are dealt in turn.

## Speed

A hundred buildings at about 58k triangles each, with no far versions. `BuildingCuller` stops drawing a building
past about 200 m from the camera and brings it back inside that. Buildings do pop in at the far end of a long
street. The fix is far versions of each building (a few thousand triangles), not built yet. Not measured on a
phone.

## The rest of the game on the bigger town

- Turf: all 25 blocks, a rival on each far corner (blocks 4, 36, 40). Simulated with nobody playing, the first raid
  reaches your corner at about 18 minutes.
- Streets, crossings, signals, lamps, props, the radar, police spawns and the district edge all follow the six
  streets each way. The skyline rings stand farther out, and the camera sees to 800 m.
- People: the four pedestrians, the town life at the tire shop and gas station, and Unc are where they were. The
  new blocks have no pedestrians yet.
