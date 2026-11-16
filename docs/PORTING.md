# Porting notes

Source: matpcul-tech/StreetEmpire `src/state.js` at a915d2b (v0.29). Read only.

## Ported

| JS | Unity |
| --- | --- |
| `CREWS` | `Assets/Data/crews.json`, `CrewDefinition` |
| `WEAPONS`, `AIM_ASSIST` | `weapons.json`, `WeaponDefinition` |
| `CAR_TYPES`, `BIKE_MODELS`, `GARAGES`, `RAMPS`, `BURN_TIME`, `BLAST_R`, `WRECK_CLEAR` | `vehicles.json`, `VehicleDefinition` |
| `MISSIONS`, `CONTACTS`, `FARE`, `COURIER`, `RACES`, `RACE_PAY`, `CALLS`, `STASHES`, `DICE` | `jobs.json`, `JobDefinition`, `ContactDefinition` |
| `stars()`, `policeStep`, `heliStep`, `HELI`, `onPlayerDown`, `addHeat` call sites | `police.json`, `PoliceConfig` |
| `BLOCK`, `GRID`, `PITCH`, costs, `DISTRICTS`, `HUSTLES`, `SHOP_ITEMS`, `SHOPS`, `HOUSES` | `world.json` |
| `createState` (grid, owners, crews), the LCG, `stars`, `addHeat`, heat decay, bail | `Assets/Scripts/Sim` |

`police.json` levels are derived from code, not a JS table: units `min(12, stars*2 + 2 at 4+)`, spawn every 5 s (3.5 s at 4+), 2 officers at 80 hp (3 at 120 hp in a 220 hp cruiser at 4+), pistols from 3 stars, helicopter at 4, roadblocks at 5.

## Parity tests

`Assets/Tests/EditMode/DataAndSimTests.cs` checks counts, the first five rng values for seed 1, block owners, rates and hustles, starting cash and crew timers, and the star thresholds against values produced by running the JS with node. These were also checked on the box by compiling `Assets/Scripts/Sim` with .NET 8 and loading the JSON (all matched). The Unity side has not been compiled yet.

## Not ported yet

Everything that moves: player, combat, cars and traffic, navigation, AI crews, capture, economy ticks, police spawning, missions, side jobs, calls. `Simulation.Step` lists the JS order to port them in, which matters for rng parity.
