# Street Empire: moving to Unity (migration plan)

Status: PLAN ONLY. Written Monday Nov 16, 2026 (America/Chicago). Nothing has been built, no repo created, no account made, no money or Meshy credits spent, and the existing repo was not changed.

Source read: `matpcul-tech/StreetEmpire` main at `a915d2b` (v0.29), cloned read only to `/workspace/StreetEmpire-ro`. Live game: https://matpcul-tech.github.io/StreetEmpire/ (stays live and untouched during the move).

---

## 0. The short version

- Engine: Unity 6.3 LTS (6000.3.28f1, released Nov 3, 2026, supported to Dec 2027), URP, new Input System. Targets in order: Android, WebGL (keeps the browser link), iOS.
- Multiplayer: Netcode for GameObjects 2.x, server authoritative, with Unity Gaming Services for sign in, lobbies and saves, and a dedicated Linux server hosted on Edgegap. Unity no longer runs its own server hosting (Multiplay ended March 31, 2026).
- Money to start: $0. Unity Personal is free under $200K revenue and funding. Google Play is already paid. Apple is $99 a year when iOS starts. Servers run about $0.07 per vCPU hour on demand, so a few dollars a month for playtests.
- The good news from the code: the current game was already written for this. `src/state.js` is a pure JSON simulation with a `net.js` "LocalSession" seam made to be swapped for an online server. That design moves to Unity almost one to one, but every line of game logic still has to be rewritten in C#.
- Models move over. Logic does not. All the Meshy GLBs carry over. The 8,411 lines of JavaScript get rewritten.

---

## 1. What carries over

### 1a. Game systems (all rewritten in C#; the design and the numbers carry over)

The current code: 8,411 lines of JavaScript in `src/`. The simulation is `state.js` (2,582 lines) and the client is `main.js` (1,561 lines). Tuning data is plain constants, so it can be converted to Unity data files (ScriptableObjects or JSON) by a script rather than retyped.

| System | Where it lives now | What carries over | Unity rewrite |
| --- | --- | --- | --- |
| Turf and crews | `state.js` `CREWS`, 9 x 9 `GRID` of 81 blocks, `HUSTLES` (81 names), `DISTRICTS` (Eastside, Midtown, Downtown, Harbor), capture ring (`ZONE_R` 7 m, `CAPTURE_TIME` 6 s), stash income, `BUILD_COST`, raids (`RAID_GATHER`), rival AI (`aiThink`) | All data and rules | Server side C# sim. Turf ownership becomes shared network state, which matters most for multiplayer |
| CREWS map | `player` (You, orange `#ff8a1a`), `reds` (Red Kings), `blues` (Blue Hand), `golds` (Gold Line), `neutral` (Locals), `police`, `civ` (Citizens). Starts: you at (0,0), each rival a 2 x 2 corner | Names, colors and start layout | One `Crew` data asset per team that points at its character models. In multiplayer each human player joins or founds a crew |
| Missions and jobs | `MISSIONS`: 19 jobs in one chain from 4 givers (`CONTACTS`: Unc, Dee, Rook, Sal, with pass and fail lines). 14 kinds: hook, collect, chop, take, fetch, deliver, tail, race, chase, beat, defend, escape, hit, own | Every title, brief line, reward and timer | A mission state machine in C#. Filmed briefs (`video.js` `BRIEF_CLIPS` with subtitle cues) carry over as data |
| Side jobs | Taxi fares (`FARE`), bike courier runs (`COURIER`), 3 street races (`RACES`), street calls (`CALLS`: incursion, ambush, deal), 20 stashes | Data and rules | Rewrite |
| Police and bust | Heat to 5 stars (`stars()`), officers that must see the crime, cruisers, tactical units at 4 stars, a helicopter (`HELI`), roadblocks at 5 stars, busted (bail 40%) or wasted (25%), respawn | All rules | Rewrite. Police AI should run on the server in multiplayer, with heat tracked per player |
| Driving | `driveCar` (speed-sensitive steering, handbrake drift, slip and skid), `CAR_TYPES` (8 cars and the bike numbers), `BIKE_MODELS` (sport, cruiser, dirt), ramps, air, crashes, fire and explosion, near misses, traffic lanes (`trafficStep`) | Tuning numbers | Rebuild on Unity physics (WheelCollider or a simple arcade raycast car tuned to these numbers). The arcade feel is easier to keep with a custom raycast car than with WheelColliders |
| Combat | `WEAPONS` (fists, knuckles, bat, pistol, shotgun with range, damage, cooldown, swing, knockback), dodge, block, melee tells, approach styles (rush, flank, retreat), aim assist, knockout tumbles | All numbers | Rewrite. Hit checks run on the server in multiplayer |
| Crew members | Recruits (`RECRUIT_COST` 100), squad formation, guards, backup (`callBackup`), guard upgrades | Rules | Rewrite with Unity NavMesh instead of the hand-made grid pathfinding (`nav`, `findPath`) |
| Shops and houses | `SHOP_ITEMS` (pistol, shotgun, bat, knuckles, patch, armor), 3 shops, safehouses, 3 garages (`GARAGE_COST`) | Data | Rewrite |
| Phone and HUD | DOM HTML and CSS in `index.html` and `main.js`: touch stick, GAS, BRAKE, DRIFT, context button, radar canvas, turf bar, wanted stars, job cards with faces, pause menu | Layout and the lessons from the real phone report (`docs/playtests/real-phone-v0.10.md`) | Rebuild in Unity UI Toolkit or uGUI, with the Input System's on-screen stick and buttons |
| Map and buildings | `city.js` builds the city in code from the grid (streets 18 m, blocks 40 m, about 520 m across). Building slots: house, store, apt, tower, shed. Hustle props (`hustle.js`) | The grid layout and the procedural rules | A C# editor script generates the same city as a Unity scene. Because the city comes from code, we can build scenes in batch mode without hand-placing in the editor |
| Vehicles look | `vehicles.js` paint, glass, lights, decals, smoke, `kit.js` paint masks and far LODs | The models | URP Shader Graph for the paint and night lights, plus LODGroup for the `.far` and `.dist` files |
| Characters and animation | `chars.js`, `retarget.js` (plays 18 Quaternius CC0 clips on Meshy Mixamo rigs), `grip.js` (finger curl on weapons) | The models and the 18 CC0 clips | Mostly deleted. Unity's Humanoid avatar retargets any Mixamo-style rig automatically, which replaces `retarget.js`. Grip becomes a hand pose or Animation Rigging |
| Camera moments | `cine.js`, `moments.js` (slow motion, orbit shots, headline cards) | The ideas | Cinemachine and Timeline |
| Clips | `clips.js` records MP4 clips in the browser | The idea | No built-in Unity runtime recorder on phones. Needs a store plugin or use the phone's own screen recorder. Open item, low priority |
| Radio and sound | `radio.js` (built-in WebAudio drum machine stations plus `public/radio/stations.json` tracks), `audio.js` procedural sounds | Track files and station list | WebAudio synth stations get rendered to audio files once, then play as normal AudioSources |
| Weather, grade, fx | `weather.js`, `grade.js`, `fx.js` | Look targets | URP post processing Volume and particle systems |
| Saves | Whole sim state in `localStorage` | The save shape | Local save file for single player, UGS Cloud Save for multiplayer profiles |
| Network seam | `net.js` LocalSession with `send(action)`, `input()`, `tick()`, events | The interface itself | Becomes the Netcode boundary: clients send actions, the server ticks the sim |
| Tests | 12 node test scripts (`tools/*-test.mjs`: missions, jobs, bust, combat, bike, collision, hustle, city, chaos, side, retarget, mask) | What they check | Unity Test Framework EditMode tests on the C# sim. These run in batch mode in CI with no graphics |
| Quality tiers | potato, low, full, phone auto settings, `?low` | The tiers | URP quality levels and adaptive performance |

### 1b. Assets

What ports directly:

- Static GLBs (buildings, cars, bikes) import with Unity's official glTFast package (`com.unity.cloud.gltfast`), including the far and dist LOD files.
- Rigged characters (Meshy, Mixamo skeleton) port, with one conversion step. glTFast imports skinned GLBs but does not create a Humanoid avatar, so each rigged GLB gets converted to FBX (Blender in batch mode), then Unity sets Animation Type to Humanoid. Source: https://docs.unity3d.com/Packages/com.unity.cloud.gltfast@6.21/manual/features.html and https://github.com/atteneder/glTFast/issues/391
- Media: the 19 filmed briefs plus `unc-intro` and `victory` (MP4 files; WebM copies are not needed), the 4 job card faces, title and key art, icons, radio tracks.
- Third-party models are CC0 (Kenney cars, Quaternius people and the 18 animation clips), so they carry over with their license files.

Inventory, with paths:

| Group | In the game repo (`public/models/kit/`) | Working files (`/workspace/meshy/street-empire/`) | Notes |
| --- | --- | --- | --- |
| Buildings | 9 kit buildings, each with `.dist` and `.far` LODs (house-01 bungalow, house-02 two story, house-03 duplex, apt-01 stucco, apt-02 brick, tower-01 glass office, tower-02 brick highrise, shed-01 corrugated, shed-02 brick industrial) plus `store-liquor-blank.glb`. 16 MB | `buildings/` with 1024 texture copies, previews and verification files | Some still have known flaws in the redo queue (house-01 roof holes, house-02 balcony glare, apt-01 corner hole). Fix those before or during import |
| Cars | 8 kit cars plus far LODs: sedan v11, muscle coupe v5, SUV v3, van v5, taxi v1, police v7, lowrider v4, pickup. 5.4 MB | `sedan-01`, `coupe-01`, `suv-01`, `van-01`, `taxi-01`, `police-01`, `lowrider-01`, `pickup-01`, clean input sedan | The sedan still has the documented UV and non-manifold issues (NOT READY in the FINISH review). Wheels must be separate objects to spin and steer in Unity. Check each car |
| Motorcycles | sportbike, cruiser, dirtbike, each with far LOD. 3.8 MB | `motorcycles/` (raw and 30k tri 1024 copies) | Not articulated. Wheels and bars need splitting to move |
| Player v2 | `characters/player.glb` | `character-v2-2026-11-13/` (rigged 1024 and 2048, lowpoly) | Mixamo skeleton, 66 joints. Ports to Humanoid |
| Unc v2 | `characters/unc.glb` | `character-v2-2026-11-13/unc-v2-*`, older `character-06-unc/` | Ports to Humanoid |
| Dee, Rook, Sal | `characters/dee.glb`, `rook.glb`, `sal.glb` | `contacts-2026-11-16/sovereign-run/` (15,000 tris each) | Dee and Rook have 66 joints. Sal has 50 and no index or ring finger bones. Unity Humanoid treats fingers as optional, so Sal works but cannot curl those fingers. Rook went through the arm fix |
| Older team set | 18 more files already in `characters/` (crew, reds, blues, golds, locals, cop) | `character-07` to `character-21` | These are the earlier team models. The v3 batch below replaces them |
| Teams v3 (18 new) | Not in the repo | `teams-v3-plan/refs/` has the 18 approved reference images: crew m1 m2 f1, reds m1 m2 f1, blues m1 m2 f1, golds m1 m2 f1, locals m1 m2 f1 f2, cops m1 m2. Plan in `teams-v3-plan/PLAN.md` | Not generated. 720 credits approved, not spent. The Meshy output (GLB, Mixamo rig, A pose, 15k tris) is exactly what Unity wants, so the batch does not need to wait for the engine move |

Must be rebuilt for Unity: the car paint mask shader, night lights and glass material, the hand grip system, and the building near-fade and texture tricks in `city.js`. These are visual code, not models.

---

## 2. Recommended Unity setup

| Item | Choice | Why and source |
| --- | --- | --- |
| Version | Unity 6.3 LTS, currently 6000.3.28f1 (released Nov 3, 2026) | The newest LTS, supported until Dec 2027. 6.0 LTS ended standard support Oct 16, 2026, and 6.6 is a short-lived update release. https://unity.com/releases/unity-6/support and https://endoflife.date/unity |
| Render pipeline | URP (Universal Render Pipeline) | Built for mobile and WebGL. One pipeline across Android, iOS and web |
| Input | Input System package (new) | One action map for touch, keyboard, mouse and gamepad, with built-in on-screen stick and buttons |
| Networking | Netcode for GameObjects 2.x with Unity Transport (WebSockets turned on for the web build) | Section 3 |
| Model import | glTFast for static GLB, FBX plus Humanoid for characters | Section 1b |
| Camera | Cinemachine | Third person follow, car chase cam, cut scenes |
| Content loading | Addressables | Phones and web download the city in pieces |
| Targets | 1. Android (AAB for Play). 2. WebGL at the same GitHub Pages address, so the current link keeps working. 3. iOS | Android first because the Play account already exists. WebGL needs a WebGL 2 browser. iOS needs a Mac with Xcode 16 for the final build (or a cloud Mac). Requirements: https://docs.unity3d.com/6000.3/Documentation/Manual/system-requirements.html |

Web note: the QA Lead's finding still applies. The Unity web build must show a clear message when a browser cannot start 3D graphics, instead of dead buttons.

---

## 3. Multiplayer

### What the game needs

A GTA-style open city with crews means a shared, persistent-feeling world: turf ownership, cash and police heat must be decided in one place and be hard to cheat. Cars move fast and need smooth syncing. Players are on phones and in browsers. The current sim is already built to run on an authoritative server.

### The options

| Stack | How it works | Pricing and free tier (official) | Fit for Street Empire |
| --- | --- | --- | --- |
| Netcode for GameObjects (NGO) plus Unity Gaming Services (UGS) | Unity's own free networking library. Can run client-server (a host or a dedicated server) or Distributed Authority (Unity's cloud passes ownership between players, no server of our own). UGS adds Authentication, Lobby, Relay, Matchmaker, Cloud Save | NGO: free. Authentication: free. Relay: first 50 average monthly CCU free, then $0.16 per extra average CCU, plus 3 GiB per CCU free up to 150 GiB a month. Distributed Authority: 6,000 connectivity hours a month free, then $0.001 per hour, 100 GiB egress free. Lobby: 10 GiB a month free per region group, then $0.09 per GiB. Cloud Save: 5 GiB storage and 1M reads and writes a month free. https://unity.com/products/gaming-services/pricing | Best fit. First party, free to start, works on Android, iOS and WebGL (WebSockets; a browser cannot host). Weak spot: no built-in client prediction, so cars need owner-driven movement with server checks |
| Multiplay (old Unity server hosting) | Was Unity's dedicated server hosting | Unity deprecated it April 1, 2026 and removed its features from 6.3 LTS. Rocket Science Group relaunched Multiplay on July 14, 2026, with no public pricing (sales call). https://status.unity.com/info_notices/362941 and https://discussions.unity.com/t/end-of-services/1698428 | Not recommended. Replace with a third-party host. Unity Matchmaker now works with third-party hosts: https://discussions.unity.com/t/unity-matchmaker-third-party-hosting-support-is-now-generally-available/1706478/1 |
| Edgegap (hosting for any of these) | Starts a dedicated Linux server near players, bills by the minute | Free trial with no card (1.5 vCPU, 1 deployment, 60 min cap). Edge Cloud $0.00115 per vCPU minute ($0.069 per vCPU hour), $0 when idle, $0.10 per GB egress, 24 hour max run per container. Private always-on host $280 a month on a 12 month term or $350 month to month (16 vCPU, 6 TB egress). https://edgegap.com/resources/pricing | Recommended host |
| Photon Fusion 2 | Commercial netcode with excellent prediction and lag compensation, run in Photon's cloud | Dev: 20 CCU free. Launch: 100 CCU free for one app, 100 CCU for $95 once a year, 500 CCU $125 a month, 1,000 CCU $250 a month, 2,000 CCU $500 a month. https://www.photonengine.com/fusion/pricing | Strong runner-up, best car feel. But on WebGL Photon recommends Shared mode, where clients own their own state and server modes are off by default. That fights the server-authoritative turf and cash design. https://doc.photonengine.com/fusion/v2/fusion-choose |
| Mirror or FishNet | Free open source libraries, you host the servers yourself | Mirror: free, MIT (https://github.com/MirrorNetworking/Mirror). FishNet: free for commercial use with no CCU limits. FishNet Pro is $50 on the Asset Store (https://fish-networking.gitbook.io/docs/overview/readme/legal-restrictions, https://assetstore.unity.com/packages/tools/network/fishnet-pro-networking-evolved-287711) | Workable and free, and FishNet has prediction. But community support only, and no matching sign in, lobby or save services. More pieces to glue together for a one-person project |

### Recommendation

**Netcode for GameObjects 2.x, server authoritative, with UGS Authentication, Lobby and Cloud Save, and dedicated Linux servers on Edgegap.**

- One server runs one city. It owns turf, cash, heat, police AI and hit checks. That is exactly the role `state.js` was written for.
- Each player owns their own movement and car for smoothness, and the server checks it (speed limits, positions).
- Start small: about 8 to 16 players per city instance, which is a number to confirm in playtests, not a promise.
- During development the server build runs on the shared computer for free, in batch mode with no graphics.
- If car syncing feels bad in testing, Photon Fusion 2 is the fallback. Distributed Authority is the cheap way to run early friend tests with no server at all.

---

## 4. Costs

| Item | Cost | Source |
| --- | --- | --- |
| Unity Personal | $0. Allowed while revenue and funding stay under $200K in the last 12 months. Splash screen optional in Unity 6 | https://unity.com/products and https://unity.com/products/unity-personal |
| Unity Pro (only if over $200K) | $2,310 a year per seat prepaid, or $210 a month | https://unity.com/products/pricing-updates |
| Runtime fee | None. Unity canceled the Runtime Fee for games on Sept 12, 2024 and went back to seat pricing | https://unity.com/blog/unity-is-canceling-the-runtime-fee |
| UGS (sign in, lobby, saves) | $0 within free tiers (Section 3) | https://unity.com/products/gaming-services/pricing |
| Game servers, playtests | Edgegap on demand. Example: a 2 vCPU city server for 40 hours a month is about $5.52 (2 x $0.069 x 40), plus egress at $0.10 per GB | https://edgegap.com/resources/pricing |
| Game servers, always-on city | Edgegap private host, $280 a month (12 month term) or $350 month to month. Running on-demand nonstop instead is about $99 a month for 2 vCPU, but each container stops after 24 hours | https://edgegap.com/resources/pricing |
| Builds (CI) | GitHub Actions standard runners are free for public repos. Unity Build Automation free tier: 200 Windows, 100 Linux and 100 Mac minutes a month, then $0.02 a minute (Micro) | https://docs.github.com/en/billing/reference/actions-runner-pricing and https://unity.com/products |
| Google Play | $0 new. The account already exists ($25 one-time fee already paid) and is declared as a trader | https://support.google.com/googleplay/android-developer/answer/6112435 |
| Apple Developer Program | $99 a year, needed only when iOS starts (Phase 4) | https://developer.apple.com/help/account/membership/program-enrollment/ |
| Meshy, 18 team characters | 720 credits (18 x 40, Meshy 7.1 High Detail). Balance 1,667, so 947 left after. Approved, not spent. No new cash | Meshy account balance as reported |
| Optional | FishNet Pro $50 once, or Photon 100 CCU $95 a year, only if that stack is chosen | Links in Section 3 |

**Totals**

- Phases 1 and 2 (build and test): **$0 cash**, plus 720 Meshy credits from the existing balance.
- Public multiplayer playtests: about **$5 to $25 a month** in server time, depending on hours.
- iOS: **$99 a year** once Phase 4 starts.
- An always-on shared city at launch: **$280 to $350 a month** for one private host. Only if that is the model Matthew wants.

---

## 5. How we would work

**Can the Unity Editor run here?** Not well as a window. The shared computer runs Debian 13 with no GPU, about 15 GB of RAM (mostly in use) and about 31 GB of free disk. Unity's Linux Editor officially supports only Ubuntu 22.04 and 24.04 with an Nvidia or AMD GPU, on real hardware, not in a container (https://docs.unity3d.com/6000.3/Documentation/Manual/system-requirements.html). The QA Lead also just found that this computer's browser cannot create a WebGL context. So the visual editor is not a working surface here.

**Headless and batch mode.** Unity does run with `-batchmode -nographics` for builds and tests, and the Linux dedicated server build needs no GPU. The catch is the license: command line activation is only for Unity Pro. A Personal license has to be activated by signing in to Unity Hub (https://docs.unity3d.com/6000.3/Documentation/Manual/ManagingYourUnityLicense.html).

**The plan: build in the cloud, not on a local editor.**

1. Matthew signs in to Unity Hub once (on his own computer, or on this computer's desktop with him typing the password) and adds a free Personal license. That makes a `Unity_lic.ulf` file. Then he signs out, which returns the seat.
2. GameCI (`game-ci/unity-builder` on GitHub Actions) uses that file plus his Unity email and password, stored as three GitHub secrets (`UNITY_LICENSE`, `UNITY_EMAIL`, `UNITY_PASSWORD`), to build Android, WebGL and the Linux server on Ubuntu runners, and to run the tests. Matthew enters these secrets himself. https://game.ci/docs/github/activation
3. iOS builds use a GitHub macOS runner or Unity Build Automation's 100 free Mac minutes, since iOS needs Xcode on a Mac.
4. I write C# code and editor scripts that build scenes and prefabs from code (the city already comes from a grid). Every change goes through a pull request. A WebGL build deploys to a preview address, so Matthew plays and reviews on his phone. Nothing merges or deploys to the main link without his OK.
5. Unity Build Automation is the backup if GameCI activation breaks. It builds straight from the repo with no local editor, on the free minutes above.

**What still needs a human with a real screen:** final look reviews, feel of driving and combat, and on-device phone tests. Matthew's phone (or a cheap Android test phone) is the real test device. If heavy hand-editing in the Unity Editor ever becomes necessary, that needs a Windows or Mac machine with a GPU, which is an open decision for later and not needed to start.

**Accounts Matthew must create or sign in to**

1. **Unity ID** (create, free): https://id.unity.com. Used for the Editor license, Unity Cloud and UGS.
2. **Unity Hub sign-in, once**, to activate the free Personal license and make the `.ulf` file for builds. The Personal license asks a short eligibility question.
3. **Unity Cloud project** (same Unity ID): turn on Authentication, Lobby and Cloud Save for multiplayer in Phase 2.
4. **GitHub** (existing `matpcul-tech`): his OK to create the new Unity repo, and adding the three Actions secrets himself.
5. **Edgegap** (create in Phase 2): free trial with no card first, a card later for real playtests.
6. **Google Play Console** (existing): a new app entry for the Unity build, or an update to an existing listing, when Android testing starts.
7. **Apple Developer Program** (create in Phase 4): $99 a year, Apple ID with two-factor sign in.

Optional, only if chosen instead: a Photon account.

---

## 6. Phased roadmap

Effort is rough and assumes I do the coding with Matthew reviewing builds on his phone. "Weeks" means calendar weeks of steady work, not promises.

**Phase 0. Setup (2 to 4 days after accounts exist)**
- New repo (name and visibility decided by Matthew), Unity 6.3 LTS project with URP and the Input System, GameCI workflows for WebGL, Android and tests, and the `.ulf` license secret.
- Asset pipeline script: GLB to Unity for buildings, cars and bikes (glTFast), GLB to FBX to Humanoid for characters, LODGroups from `.far` and `.dist`.
- A data export script that turns CREWS, MISSIONS, CONTACTS, HUSTLES, WEAPONS, CAR_TYPES, RACES and SHOP_ITEMS from `state.js` into Unity data assets.
- Exit check: an empty scene builds to WebGL and Android in CI.

**Phase 1. Single player vertical slice (3 to 5 weeks)**
- One neighborhood: Eastside, about 3 x 3 blocks generated from the grid, with the kit buildings.
- On foot movement, third person camera, phone controls (stick, sprint, jump, hit, context).
- Driving: one or two cars (sedan, muscle coupe) and one bike with the current tuning numbers, traffic on a few streets, crash damage.
- One job: "Bag Back" from Unc, with its filmed brief and subtitles.
- Team characters: player v2, Unc, and the 18 v3 team models (once generated) on Humanoid with the CC0 clips, so every crew reads at a glance.
- Basic police heat (1 to 2 stars and a bust) and a clear "cannot start 3D graphics" message on the web.
- Exit check: Matthew plays it on his phone and in a browser. The old link stays on the Three.js game.

**Phase 2. Multiplayer (4 to 6 weeks)**
- Move the slice's sim to an NGO server: turf, cash, heat and hits on the server, each player owning their own movement and car.
- UGS sign in (anonymous first), Lobby to join a city, Cloud Save for player profile and crew.
- Linux server build in CI, running on this computer for dev and on Edgegap for playtests.
- Crews as teams: players pick or found a crew. Team colors and silhouettes on the v3 models.
- Exit check: 4 to 8 people drive and fight in one city on phones and browsers.

**Phase 3. Port the rest (8 to 12 weeks)**
- Full 9 x 9 city and all four districts, all 8 cars and 3 bikes, the rest of the 19 jobs and all givers (Dee, Rook, Sal), side jobs (fares, courier, races, street calls), full police (cruisers, tactical, helicopter, roadblocks), all weapons and combat polish, recruits and guards, shops, safehouses and garages, radio, weather and night, camera moments, the full HUD and pause menu, and the C# versions of the 12 test scripts.
- Decide how the single player story and the shared city fit together (story on your own, turf war online is the simple split).

**Phase 4. Release (2 to 4 weeks)**
- Play Store internal testing, then closed testing on the existing account. Apple enrollment and TestFlight. The Unity WebGL build replaces the old one at https://matpcul-tech.github.io/StreetEmpire/ only when Matthew says so.
- Performance passes on mid-range phones, store listings, privacy and data safety forms.

Rough total: about 4 to 6 months to a full multiplayer release, with a playable slice in about 1 month and the first multiplayer test in about 2 to 3 months.

---

## 7. Open decisions for Matthew (4)

1. **New repo, public or private?** Public keeps GitHub build minutes free and unlimited. Private gives a smaller free allowance each month and hides the code. (The current repo is public.)
2. **How big is a city?** Small sessions of about 8 to 16 players on on-demand servers (a few dollars a month in testing) or one always-on shared city ($280 to $350 a month for a private host at launch).
3. **Start the 720-credit Meshy batch now?** The Meshy output (GLB with a Mixamo rig) works in Unity as is, so the hold is no longer needed. Balance goes from 1,667 to 947.
4. **When does the Unity web build take over the current link?** After the Phase 1 slice, or not until Phase 3 matches the current game. Recommended: keep the Three.js game live until Phase 3.
