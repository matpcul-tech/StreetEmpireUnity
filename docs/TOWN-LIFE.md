# Town life

People on a block who work, shop, and walk in and out of doors. One system, driven by markers.

## The pieces

| File | What it does |
| --- | --- |
| `Assets/Scripts/Runtime/Game/ActivitySpot.cs` | One place a person can do something: a Door, a Work spot, or a Visit spot (pump, waiting area). Holds how long a use lasts, opening hours, how many can use it at once, and an optional animation trigger. |
| `Assets/Scripts/Runtime/Game/Townie.cs` | The person. Picks an open, free spot, walks to it round the sidewalk, uses it, picks another. Workers keep to their shop and go inside when it closes. Runs when hit, like a pedestrian. |
| `Assets/Scripts/Runtime/Game/TownLife.cs` | The manager on the Game object. Spawns townies from templates, routes them, brings them back out of doors, and switches a block's people off when the player is far away. |
| `Assets/Scripts/Editor/TownLifeBuilder.cs` | Writes the spots and templates into the scene when it is built. The layouts live here. |

## Which blocks are alive

A block comes to life when one of its buildings has an entry in `TownLifeBuilder.Shops`. Right now: `shop-auto-garage` (the tire shop) and `shop-gas-station`. Any model with `tire` or `garage` in its name uses the tire shop layout and any with `gas` uses the gas station layout, so a replaced file (a v2) stays alive. The spot positions were measured from the files in the repo on Nov 21, 2026 (two bay garage, pumps under the front of the canopy); when a new tire shop or gas station file lands, re-measure and update the `x` and `outFront` numbers. Every other building on that block gets a front door automatically, so residents walk between their houses and the shop.

## Bringing another business to life

Add its model name to `Shops` with hours and spots:

```csharp
["store-barber"] = new Shop
{
    open = 8f, close = 20f,
    spots = new[]
    {
        Door("front door", 0f, 0.8f),          // x across the front, how deep the door sits behind the front edge
        Work("sweeping out front", 0.5f, 1.2f, true, ""),
        Visit("waiting outside", -0.5f, 1.5f, ""),
    },
},
```

- `x` runs -1 to 1 across the front of the building's footprint (Unity local X).
- `outFront` is meters out from the front edge of the footprint. Keep it under 2.5 so nobody stands in the pedestrians' line down the middle of the sidewalk.
- Spots must sit outside the building's box collider. `TownLifeTests` fails if one is inside.
- Houses need nothing. Their door is found from the finished file's `door` material when it has one, else the middle of the front.

## Hours

Hours are game clock hours (`GameClock.Hour`; a new game starts at 17:48 and a day lasts 8 minutes). The tire shop is open 7 to 19, the gas station always. When a shop closes its workers walk in the door and stay there until it opens.

## Animations

A spot's `animTrigger` ("Work", "PumpGas") is set on the townie's Animator when they start using the spot. It does nothing until `Player.controller` has a Trigger parameter with that name wired to a clip. Until then people stand and face the spot.

## Numbers to tune

On the `TownLife` component: `workersPerShop` (2), `residentsPerBlock` (3), `maxTownies` (16), `sleepDistance` (100 m), `wakeDistance` (88 m).

## Not done yet

- No interiors. Going in a door means walking to it and going out of sight.
- People stay on their own block. No crossing streets.
- No cars at the pumps. The pump spots are ready for when vehicles return.
- Work and pump clips are not in the controller yet.
- The tire shop's bay doors are closed in the model. Open bays need the art.
