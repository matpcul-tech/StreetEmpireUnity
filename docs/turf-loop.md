# The turf loop (Eastside)

What it is: take a block, earn from it, get raided, defend it, spend the cash. Rules and numbers are the web game's
(`state.js`), run by `Assets/Scripts/Sim/Turf.cs` and put in the town by `Assets/Scripts/Runtime/Game/TurfWar.cs`.

How it plays:

- Nine blocks. You hold Corner Store Deals. Red Kings, Blue Hand and Gold Line each hold one far corner. The other
  five are the Locals'.
- Turf is implied. There is no ring on the ground and none at anyone's feet. A block is somebody's because their
  people stand on it. The crew colors are on the radar.
- Walk onto a block and its guards show. They hold their posts in the backyard until you come within 14 m, or hit
  one.
- Knock them all down, then stay on the block for 6 seconds (anywhere on its own ground, inside the sidewalk; the
  count shows as "Holding Car Wash 4.2s") and the block is yours. One guard of yours stays on it.
- Your blocks pile up a stash (rate x 0.4 with no guards, up to 1.0 with three, double when built up). Walk onto
  the block to pocket it.
- USE on a block you hold buys the next thing it needs: a second guard (100), bats (80), a third guard (100),
  the build up (200).
- Rivals bank their own stash, keep two guards on a block, and raid a neighbor when they have 150. On your block
  you get a 10 second warning. Be on the block and the raiders are bodies you fight. Be away and your guards
  fight it out by rolls. A block with nobody left on it falls in 6 seconds.
- A new player is not raided until he takes his first block, or five minutes in.
- Hold all nine and Eastside is yours.

What differs from the web game: the web walks every guard and raider as a body across the whole city. Here bodies
only exist on the block you are standing on. Fights you are not at are settled by rolls.

Not in yet: your guards fighting as bodies (they stand at their posts; the sim rolls for them), crew models for
each rival (all guards and raiders use the Locals bodies; on a block the guards are its owner's), recruits that
follow you, and the block colors painted into the radar map itself (owners show as dots).

Saved: the turf state and cash are kept between launches on the phone (PlayerPrefs `turf.v1`). Jobs and the
opening are not saved yet.

Pace: the rival timings in `Turf.cs` were set by running 300 seeded games of the rules with nobody playing.
They have not been played by a person yet.
