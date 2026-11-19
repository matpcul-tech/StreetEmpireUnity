// Screenshot set-ups for the headless WebGL capture (tools/capture_webgl.mjs --query=shot=...).
// Only acts when the page URL asks for it, so normal play is untouched:
//   shot=job     next to Unc with Bag Back started (brief, marker, timer)
//   shot=bag     Bag Back running, standing by the bag
//   shot=police  two stars, officers on the way
//   shot=car     in the sedan
//   shot=overview high view over the whole district
//   shot=cruiser two stars, standing still while a cruiser drives in
//   shot=peds    side view following a walking pedestrian (posture, feet, corners); logs each ped's lean
//   shot=legs    side view of the player: idle, then keys 2 walk, 3 sprint, 1 stand; logs the knee angles
//   shot=tour    fixed street views of the Eastside build-out, keys 1 to 8 pick the view (view=N in the URL
//                starts on one); logs the frame's draw call, batch and triangle counters where the player has them
//   shot=close   close ups of the playtest fixes, keys 1 to 9 and 0: 1 palm fronds, 2 barbershop wall, 3 bungalow
//                (white house, red roof), 4 apartment windows, 5 the hoodie, 6 Unc and the player, 7 the objective
//                marker from the game camera, 8 a yard with grass clumps, 9 a wood fence, 0 the windows at 21:00
//   shot=close2  v73 views, keys 1 to 6: 1 the HUD and the Unc prompt next to Unc (game camera), 2 the player on
//                the avenue (character fill against the road, game camera), 3 the Arm guards prompt on a held block,
//                4 street lamp pools from above at 21:00, 5 the avenue at eye height at 21:00, 6 the skyline edge
//   shot=v74     v74 report views, keys 1 to 9: 1 the avenue at 14:00, 2 the same at 19:00, 3 a street lamp and the
//                stucco apartment at 22:00, 4 the pawn shop front at 22:00, 5 the apartment close at 22:00, 6 a house
//                close by day, 7 palm fronds close by day, 8 the district edge, 9 a wood fence close by day
//   shot=v75     v75 report views, keys 1 to 9 and 0, all but 7 and 8 through the game camera with the player in
//                frame: 1 the avenue at 14:00, 2 the same at 19:00, 3 under a street lamp by the stucco apartment at
//                22:00, 4 the pawn shop front at 22:00, 5 the apartment close at 22:00, 6 the white house by day,
//                7 palm fronds close (free camera), 8 the district edge (free camera), 9 the avenue at 14:00 (for
//                the Ultra and High pair), 0 a signal crossing at 14:00 (poles on the corners), minus the liquor
//                store sign at 22:00, equals the mixed use apartment block by day
//   shot=fight   the opening scuffle with a hit landed on a mugger every 0.8 s, so the target ring, warning rings,
//                hit numbers, sparks, dust and shockwaves show on cue (game camera)
//   shot=mugger  the opening scuffle as a new game plays it (every other shot skips it)
//   gfx=high     (with any shot) start on the High graphics tier instead of Ultra
using System.Collections;
using StreetEmpire.Player;
using StreetEmpire.Vehicles;
using UnityEngine;

namespace StreetEmpire.Game
{
    public class ShotDirector : MonoBehaviour
    {
        IEnumerator Start()
        {
            string url = Application.absoluteURL ?? "";
            int i = url.IndexOf("shot=");
            if (i < 0) yield break;
            string shot = url.Substring(i + 5).Split('&')[0];
            var opening = FindFirstObjectByType<MuggerOpening>();
            if (opening != null && shot != "mugger" && shot != "fight") opening.Skip();
            // still shots keep the clock (and the sun) at the start hour, or at hour=N from the URL
            if (shot != "mugger" && shot != "fight")
            {
                GameClock.Frozen = true;
                int hq = url.IndexOf("hour="); if (hq >= 0 && float.TryParse(url.Substring(hq + 5).Split('&')[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float hh)) GameClock.FrozenHour = hh;
            }
            if (url.Contains("gfx=high"))
            {
                var gq = FindFirstObjectByType<StreetEmpire.UI.GraphicsQuality>();
                if (gq != null && StreetEmpire.UI.GraphicsQuality.Tier == StreetEmpire.UI.GraphicsQuality.Ultra) gq.Toggle();
            }
            yield return null;
            var s = GameSession.Instance; var job = FindFirstObjectByType<MissionRunner>();
            if (s == null) yield break;
            var p = s.player;
            switch (shot)
            {
                case "job":
                    p.Teleport(job.giver.position + job.giver.forward * 2.2f + job.giver.right * 0.8f, job.giver.eulerAngles.y + 180f);
                    yield return null; job.Begin();
                    break;
                case "bag":
                    job.Begin();
                    var to = job.Target; p.Teleport(to + new Vector3(0, 0.1f, -6f), 0f);
                    break;
                case "police":
                    s.AddHeat(45f);
                    break;
                case "cruiser":
                    s.AddHeat(45f);
                    break;
                case "overview":
                    var cam = Camera.main;
                    var brain = cam.GetComponent<Unity.Cinemachine.CinemachineBrain>(); if (brain) brain.enabled = false;
                    float cx = (s.spanX.x + s.spanX.y) / 2, cz = (s.spanZ.x + s.spanZ.y) / 2;
                    cam.transform.position = new Vector3(cx + 70f, 120f, cz - 150f);
                    cam.transform.LookAt(new Vector3(cx, 0, cz - 5f));
                    cam.farClipPlane = 800f;
                    break;
                case "peds":
                    StartCoroutine(FollowPed());
                    break;
                case "legs":
                    StartCoroutine(PlayerLegs(p));
                    break;
                case "tour":
                    StartCoroutine(Tour(url));
                    break;
                case "close":
                    StartCoroutine(Close(p, job));
                    break;
                case "close2":
                    StartCoroutine(Close2(p, job));
                    break;
                case "v74":
                    StartCoroutine(V74(p));
                    break;
                case "v75":
                    StartCoroutine(V75(p));
                    break;
                case "fight":
                    StartCoroutine(Fight(p));
                    break;
                case "car":
                    var car = FindFirstObjectByType<CarController>();
                    p.Teleport(car.transform.position - car.transform.right * 2.4f, car.transform.eulerAngles.y);
                    yield return null;
                    p.GetComponent<VehicleUser>().TryEnter();
                    break;
            }
            Debug.Log($"[ShotDirector] {shot}");
        }

        /// <summary>Views for the build-out shots, from the block markers: 1 an avenue at eye height, 2 a
        /// signal crossing, 3 into an open lot, 4 a block from above, 5 the backyards, 6 a stop sign crossing,
        /// 7 the whole district, 8 a curb with parked cars.</summary>
        IEnumerator Tour(string url)
        {
            var cam = Camera.main;
            var brain = cam.GetComponent<Unity.Cinemachine.CinemachineBrain>(); if (brain) brain.enabled = false;
            cam.farClipPlane = 800f;
            Vector3 B(int id) { foreach (var m in FindObjectsByType<StreetEmpire.City.BlockMarker>(FindObjectsSortMode.None)) if (m.blockId == id) return m.transform.position; return Vector3.zero; }
            var b10 = B(10); var b0 = B(0); var b20 = B(20); var b2 = B(2);
            var views = new (Vector3 pos, Vector3 at)[]
            {
                (b10 + new Vector3(28f, 1.7f, -24f), b10 + new Vector3(27f, 1.5f, 30f)),
                (b10 + new Vector3(-34f, 2.2f, 36f), b10 + new Vector3(-22f, 1.5f, 24f)),
                (b10 + new Vector3(0f, 1.8f, -27f), b10 + new Vector3(0f, 1.2f, -12f)),
                (b20 + new Vector3(-36f, 26f, -36f), b20 + new Vector3(0f, 0f, 0f)),
                (b0 + new Vector3(5.5f, 1.7f, -5.5f), b0 + new Vector3(-6f, 1.4f, 6f)),
                (b2 + new Vector3(32f, 1.8f, 12f), b2 + new Vector3(24f, 1.5f, 24f)),
                ((b0 + b20) / 2 + new Vector3(80f, 110f, -150f), (b0 + b20) / 2),
                (b20 + new Vector3(-26.5f, 1.6f, -16f), b20 + new Vector3(-24.5f, 1.0f, 12f)),
            };
            int view = 0; int q = url.IndexOf("view="); if (q >= 0 && int.TryParse(url.Substring(q + 5).Split('&')[0], out int v0)) view = Mathf.Clamp(v0 - 1, 0, views.Length - 1);
            var draws = Unity.Profiling.ProfilerRecorder.StartNew(Unity.Profiling.ProfilerCategory.Render, "Draw Calls Count");
            var batches = Unity.Profiling.ProfilerRecorder.StartNew(Unity.Profiling.ProfilerCategory.Render, "Batches Count");
            var setpass = Unity.Profiling.ProfilerRecorder.StartNew(Unity.Profiling.ProfilerCategory.Render, "SetPass Calls Count");
            var trisRec = Unity.Profiling.ProfilerRecorder.StartNew(Unity.Profiling.ProfilerCategory.Render, "Triangles Count");
            float nextLog = 2f; int shown = -1;
            while (true)
            {
                var kb = UnityEngine.InputSystem.Keyboard.current;
                if (kb != null)
                {
                    var keys = new[] { kb.digit1Key, kb.digit2Key, kb.digit3Key, kb.digit4Key, kb.digit5Key, kb.digit6Key, kb.digit7Key, kb.digit8Key };
                    for (int i = 0; i < keys.Length; i++) if (keys[i].isPressed) view = i;
                }
                cam.transform.position = views[view].pos; cam.transform.LookAt(views[view].at);
                if (view != shown) { shown = view; nextLog = Time.time + 1.5f; }
                if (Time.time > nextLog)
                {
                    nextLog = Time.time + 3f;
                    Debug.Log($"[ShotDirector] tour view {view + 1} draws {draws.LastValue} batches {batches.LastValue} setpass {setpass.LastValue} tris {trisRec.LastValue} frame {Time.smoothDeltaTime * 1000f:0} ms (software renderer here, not a phone number)");
                    Debug.Log($"[ShotDirector] tour view {view + 1} drawn once (before shadow and depth passes): {Breakdown()}");
                }
                yield return null;
            }
        }

        IEnumerator Close(PlayerMotor p, MissionRunner job)
        {
            var cam = Camera.main;
            var brain = cam.GetComponent<Unity.Cinemachine.CinemachineBrain>();
            Vector3 Block(int id) { foreach (var m in FindObjectsByType<StreetEmpire.City.BlockMarker>(FindObjectsSortMode.None)) if (m.blockId == id) return m.transform.position; return Vector3.zero; }
            var b10 = Block(10);
            Transform Prop(string model, Vector3 near)
            {
                Transform best = null; float bd = float.MaxValue;
                foreach (var m in FindObjectsByType<StreetEmpire.City.PropMarker>(FindObjectsSortMode.None))
                    if (m.model == model) { float d = (m.transform.position - near).sqrMagnitude; if (d < bd) { bd = d; best = m.transform; } }
                return best;
            }
            (Vector3 pos, Vector3 at) Facade(string model, float back, float side, float h)
            {
                StreetEmpire.City.BuildingMarker best = null; float bd = float.MaxValue;
                foreach (var m in FindObjectsByType<StreetEmpire.City.BuildingMarker>(FindObjectsSortMode.None))
                    if (m.model != null && m.model.StartsWith(model)) { float d = (m.transform.position - b10).sqrMagnitude; if (d < bd) { bd = d; best = m; } }
                if (best == null) return (b10 + new Vector3(0, 30, -30), b10);
                var c = best.GetComponent<BoxCollider>(); var bb = c ? c.bounds : new Bounds(best.transform.position, Vector3.one * 10);
                var o = bb.center - Block(best.blockId); o.y = 0;
                var n = Mathf.Abs(o.x) > Mathf.Abs(o.z) ? new Vector3(Mathf.Sign(o.x), 0, 0) : new Vector3(0, 0, Mathf.Sign(o.z));
                var t = Vector3.Cross(Vector3.up, n);
                float ext = Mathf.Abs(Vector3.Dot(bb.extents, n));
                var face = bb.center + n * ext; face.y = bb.min.y;
                return (face + n * back + t * side + Vector3.up * 1.7f, face + Vector3.up * h);
            }
            int view = 0; int shown = -1;
            while (true)
            {
                var kb = UnityEngine.InputSystem.Keyboard.current;
                if (kb != null)
                {
                    var keys = new[] { kb.digit1Key, kb.digit2Key, kb.digit3Key, kb.digit4Key, kb.digit5Key, kb.digit6Key, kb.digit7Key, kb.digit8Key, kb.digit9Key, kb.digit0Key };
                    for (int i = 0; i < keys.Length; i++) if (keys[i].isPressed) view = i;
                }
                if (view != shown)
                {
                    shown = view; GameClock.FrozenHour = view == 9 ? 21f : GameClock.StartHour;
                    bool game = view == 6;   // the marker is seen through the game camera
                    if (brain) brain.enabled = game;
                    var giver = job.giver;
                    if (view == 4 || view == 5 || view == 6)
                    {
                        if (view == 6) p.Teleport(giver.position + giver.forward * 18f + giver.right * 4f, giver.eulerAngles.y + 180f);
                        else if (view == 5) p.Teleport(giver.position + giver.forward * 0.9f, giver.eulerAngles.y + 180f);   // pushed out by his body
                        else p.Teleport(b10 + new Vector3(28f, 0.4f, -24f), 0f);
                    }
                    Debug.Log($"[ShotDirector] close view {(view + 1) % 10}");
                }
                (Vector3 pos, Vector3 at) v = (cam.transform.position, cam.transform.position + cam.transform.forward);
                switch (view)
                {
                    case 0: { var palm = Prop("palm-short", b10 + new Vector3(0, 0, -27f)); var a = palm ? palm.position : b10; v = (a + new Vector3(3.5f, 4.6f, -3.5f), a + new Vector3(0, 7.2f, 0)); break; }
                    case 1: v = Facade("store-barber", 6.5f, 1.5f, 2.6f); break;
                    case 2: v = Facade("house-02", 8f, 2f, 2.4f); break;   // v74: house-01 is not shipped
                    case 3: v = Facade("apt-02", 9f, 2.5f, 5f); break;
                    case 4: { var t = p.transform; v = (t.position + t.forward * 1.7f + t.right * 0.4f + Vector3.up * 1.35f, t.position + Vector3.up * 1.15f); break; }
                    case 5: { var g = job.giver; v = (g.position + g.forward * 0.6f + g.right * 3.2f + Vector3.up * 1.5f, g.position + g.forward * 0.6f + Vector3.up * 1.1f); break; }
                    case 6: break;
                    case 7: { var c = Prop("grass-clump", b10 + new Vector3(0, 0, -27f)); var a = c ? c.position : b10; v = (a + new Vector3(2.6f, 1.3f, -2.6f), a + new Vector3(0, 0.15f, 0)); break; }
                    case 8: { var f = Prop("wood-fence", b10 + new Vector3(0, 0, -27f)); var a = f ? f.position : b10; var fw = f ? f.forward : Vector3.forward; v = (a + fw * 2.6f + (f ? f.right : Vector3.right) * 0.8f + Vector3.up * 1.4f, a + Vector3.up * 0.9f); break; }
                    case 9: v = Facade("apt-02", 14f, 4f, 5f); break;
                }
                if (view != 6) { cam.transform.position = v.pos; cam.transform.LookAt(v.at); }
                yield return null;
            }
        }

        /// <summary>Lands a hit on the nearest standing mugger every 0.8 s (alternating light and heavy, never the
        /// knockout until the last), turning the player to face him, so every fight effect shows for the capture.</summary>
        IEnumerator Fight(PlayerMotor p)
        {
            float wait = 0; while (Brawler.All.Count == 0 && wait < 30f) { wait += Time.deltaTime; yield return null; }
            int n = 0;
            while (true)
            {
                yield return new WaitForSeconds(0.8f);
                Brawler best = null; float bd = float.MaxValue;
                foreach (var b in Brawler.All)
                {
                    if (b == null) continue; var h = b.GetComponent<Health>(); if (h == null || h.Down) continue;
                    float d = (b.transform.position - p.transform.position).sqrMagnitude; if (d < bd) { bd = d; best = b; }
                }
                if (best == null) continue;
                var to = best.transform.position - p.transform.position; to.y = 0;
                if (to.sqrMagnitude > 0.01f) p.transform.rotation = Quaternion.LookRotation(to);
                var hp = best.GetComponent<Health>();
                float dmg = n++ % 2 == 0 ? 9f : 16f; if (hp.hp - dmg <= 0 && Brawler.All.Count > 1 && n < 12) dmg = Mathf.Max(1f, hp.hp - 5f);
                hp.Damage(dmg, p.gameObject);
                Debug.Log($"[ShotDirector] fight hit {dmg} on {best.name} hp {hp.hp}");
            }
        }

        IEnumerator Close2(PlayerMotor p, MissionRunner job)
        {
            var cam = Camera.main;
            var brain = cam.GetComponent<Unity.Cinemachine.CinemachineBrain>();
            Vector3 Block(int id) { foreach (var m in FindObjectsByType<StreetEmpire.City.BlockMarker>(FindObjectsSortMode.None)) if (m.blockId == id) return m.transform.position; return Vector3.zero; }
            var b10 = Block(10); var b0 = Block(0); var b20 = Block(20);
            Transform Prop(string model, Vector3 near)
            {
                Transform best = null; float bd = float.MaxValue;
                foreach (var m in FindObjectsByType<StreetEmpire.City.PropMarker>(FindObjectsSortMode.None))
                    if (m.model == model) { float d = (m.transform.position - near).sqrMagnitude; if (d < bd) { bd = d; best = m.transform; } }
                return best;
            }
            var arm = FindFirstObjectByType<ArmGuards>();
            int view = 0; int shown = -1;
            while (true)
            {
                var kb = UnityEngine.InputSystem.Keyboard.current;
                if (kb != null)
                {
                    var keys = new[] { kb.digit1Key, kb.digit2Key, kb.digit3Key, kb.digit4Key, kb.digit5Key, kb.digit6Key };
                    for (int i = 0; i < keys.Length; i++) if (keys[i].isPressed) view = i;
                }
                if (view != shown)
                {
                    shown = view; GameClock.FrozenHour = view == 3 || view == 4 ? 21f : GameClock.StartHour;
                    bool game = view <= 2;
                    if (brain) brain.enabled = game;
                    var g = job.giver;
                    if (view == 0) p.Teleport(g.position + g.forward * 2.2f + g.right * 0.6f, g.eulerAngles.y + 180f);
                    else if (view == 1) p.Teleport(b10 + new Vector3(28f, 0.4f, -24f), 0f);
                    else if (view == 2 && arm != null && arm.ownedBlocks.Length > 0) p.Teleport(arm.ownedBlocks[0] + new Vector3(arm.blockHalf - 4f, 0.4f, 0f), -90f);
                    Debug.Log($"[ShotDirector] close2 view {view + 1}" + (arm != null ? $" arm prompt {arm.Showing}" : ""));
                }
                if (view >= 3)
                {
                    (Vector3 pos, Vector3 at) v;
                    if (view == 3) { var l = Prop("street-lamp-1", b10 + new Vector3(0, 0, -27f)); var a = l ? l.position : b10; v = (a + new Vector3(7f, 7f, -7f), a + new Vector3(0, 0.2f, 0)); }
                    else if (view == 4) v = (b10 + new Vector3(28f, 1.7f, -24f), b10 + new Vector3(27f, 1.5f, 30f));
                    else { var c = (b0 + b20) / 2; v = (c + new Vector3(60f, 38f, -110f), c + new Vector3(-40f, 0f, 120f)); cam.farClipPlane = 800f; }
                    cam.transform.position = v.pos; cam.transform.LookAt(v.at);
                }
                yield return null;
            }
        }

        IEnumerator V75(PlayerMotor p)
        {
            var cam = Camera.main;
            var brain = cam.GetComponent<Unity.Cinemachine.CinemachineBrain>();
            var rig = FindFirstObjectByType<StreetEmpire.Player.CameraRig>();
            Vector3 Block(int id) { foreach (var m in FindObjectsByType<StreetEmpire.City.BlockMarker>(FindObjectsSortMode.None)) if (m.blockId == id) return m.transform.position; return Vector3.zero; }
            var b10 = Block(10); var b0 = Block(0); var b20 = Block(20);
            Transform Prop(string model, Vector3 near)
            {
                Transform best = null; float bd = float.MaxValue;
                foreach (var m in FindObjectsByType<StreetEmpire.City.PropMarker>(FindObjectsSortMode.None))
                    if (m.model == model) { float d = (m.transform.position - near).sqrMagnitude; if (d < bd) { bd = d; best = m.transform; } }
                return best;
            }
            // the street front of one building of the model: its foot, outward normal, tangent and height. The front is
            // the holder's forward or back, whichever points away from its block; of every copy of the model, the one
            // with the fewest palms, poles and lamps between the camera spot (back m out) and the wall
            var blockers = new System.Collections.Generic.List<Vector3>();
            foreach (var m in FindObjectsByType<StreetEmpire.City.PropMarker>(FindObjectsSortMode.None))
                if ((m.model != null && (m.model.StartsWith("palm") || m.model.StartsWith("tree") || m.model.Contains("pole") || m.model.Contains("lamp") || m.model.Contains("signal"))) || m.kind == "tree" || m.kind == "streetlight" || m.kind == "traffic signal") blockers.Add(m.transform.position);
            // v75g: and any other tall thin thing with a renderer (trunks, poles and posts not tagged as props)
            foreach (var r in FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
            {
                var bb = r.bounds; if (bb.size.y < 3f || Mathf.Max(bb.size.x, bb.size.z) > 1.2f) continue;
                var n = r.name.ToLowerInvariant(); if (n.Contains("palm") || n.Contains("tree") || n.Contains("trunk") || n.Contains("pole") || n.Contains("post") || n.Contains("lamp")) blockers.Add(new Vector3(bb.center.x, 0f, bb.center.z));
            }
            (Vector3 face, Vector3 n, Vector3 t, float h) Face(string model, float back = 9f)
            {
                (Vector3, Vector3, Vector3, float) best = (b10, Vector3.back, Vector3.right, 8f); float bestScore = float.MaxValue;
                foreach (var m in FindObjectsByType<StreetEmpire.City.BuildingMarker>(FindObjectsSortMode.None))
                {
                    if (m.model == null || !m.model.StartsWith(model)) continue;
                    var c = m.GetComponent<BoxCollider>(); var bb = c ? c.bounds : new Bounds(m.transform.position, Vector3.one * 10);
                    var o = bb.center - Block(m.blockId); o.y = 0;
                    var n = m.transform.forward; n.y = 0; n.Normalize(); if (Vector3.Dot(n, o) < 0) n = -n;
                    var face = bb.center + n * Mathf.Abs(Vector3.Dot(bb.extents, new Vector3(Mathf.Abs(n.x), 0, Mathf.Abs(n.z)))); face.y = bb.min.y;
                    var cam0 = face + n * back; float score = 0;
                    foreach (var q in blockers)
                    {
                        var qq = q; qq.y = face.y; var seg = face - cam0; float u = Mathf.Clamp01(Vector3.Dot(qq - cam0, seg) / seg.sqrMagnitude);
                        if ((cam0 + seg * u - qq).magnitude < 1.6f) score += 1f;
                    }
                    score += (bb.center - b10).magnitude * 0.002f;
                    if (score < bestScore) { bestScore = score; best = (face, n, Vector3.Cross(Vector3.up, n), bb.size.y); }
                }
                return best;
            }
            // v75: every street view is the game's own follow camera behind the player (no free debug camera), so
            // the player is in each one; the orbit is turned to look where the view needs. Only the palm close up (7)
            // and the high district edge view (8) are free cameras.
            // v75c (Matthew's second notes), on the numpad: 14 07:20 street, 15 09:36 skyline, 16 08:23 signal close,
            // 17 02:08 lamp pool (the shot 3 lamp), 18 05:55 street wide, 19 05:55 pawn shop sidewalk, 20 the EMPIRE
            // graffiti house, 21 a parked sedan close up. v75d (Matthew's v2 files): 22 the pawn shop by day, 23 the
            // apartment by day, 24 the white house at 22:00 (Numpad9, Numpad0, NumpadPeriod). v75e: 25 the white
            // house and its sedan at 19:24 (Matthew's phone dusk), 26 a pedestrian close up (NumpadMultiply, NumpadPlus)
            float[] hours = { 14f, 19f, 22f, 22f, 22f, 14f, 14f, 14f, 14f, 14f, 22f, 14f, 22f, 7.333f, 9.6f, 8.383f, 2.133f, 5.917f, 5.917f, 14f, 14f, 14f, 14f, 22f, 19.4f, 14f, 14f };
            int view = 0; int shown = -1;
            // v75c: draw calls per view, a second and a half after each switch
            var draws = Unity.Profiling.ProfilerRecorder.StartNew(Unity.Profiling.ProfilerCategory.Render, "Draw Calls Count");
            var batches = Unity.Profiling.ProfilerRecorder.StartNew(Unity.Profiling.ProfilerCategory.Render, "Batches Count");
            var setpass = Unity.Profiling.ProfilerRecorder.StartNew(Unity.Profiling.ProfilerCategory.Render, "SetPass Calls Count");
            float drawLog = float.MaxValue;
            void Game(Vector3 at, Vector3 look, float pitch, float radius)
            {
                look.y = 0; float yaw = Quaternion.LookRotation(look.normalized).eulerAngles.y;
                at.y = 0.4f; p.Teleport(at, yaw);
                if (brain) brain.enabled = true;
                if (rig) { rig.radius = radius; rig.orbit.Radius = radius; rig.orbit.HorizontalAxis.Value = Mathf.DeltaAngle(0f, yaw); rig.orbit.VerticalAxis.Value = pitch; }
            }
            StreetEmpire.Game.Ped hoodie = null;
            StreetEmpire.Game.Ped HoodiePed()
            {
                if (hoodie != null) return hoodie;
                float bd = float.MaxValue; StreetEmpire.Game.Ped any = null; float ad = float.MaxValue;
                foreach (var q in FindObjectsByType<StreetEmpire.Game.Ped>(FindObjectsSortMode.None))
                {
                    float d = (q.transform.position - b10).sqrMagnitude;
                    bool gray = false; foreach (var r in q.GetComponentsInChildren<SkinnedMeshRenderer>()) if (r.sharedMaterial && r.sharedMaterial.name.Contains("ped-m-1")) gray = true;
                    if (gray && d < bd) { bd = d; hoodie = q; }
                    if (d < ad) { ad = d; any = q; }
                }
                if (hoodie == null) { Debug.Log("[ShotDirector] no ped-m-1 found, the nearest pedestrian instead"); hoodie = any; }
                return hoodie;
            }
            Vector3 PoleOf(Transform signal) { var c = signal.Find("Pole collider"); return c ? c.position : signal.position; }
            while (true)
            {
                var kb = UnityEngine.InputSystem.Keyboard.current;
                if (kb != null)
                {
                    var keys = new[] { kb.digit1Key, kb.digit2Key, kb.digit3Key, kb.digit4Key, kb.digit5Key, kb.digit6Key, kb.digit7Key, kb.digit8Key, kb.digit9Key, kb.digit0Key, kb.minusKey, kb.equalsKey, kb.backspaceKey,
                        kb.numpad1Key, kb.numpad2Key, kb.numpad3Key, kb.numpad4Key, kb.numpad5Key, kb.numpad6Key, kb.numpad7Key, kb.numpad8Key, kb.numpad9Key, kb.numpad0Key, kb.numpadPeriodKey, kb.numpadMultiplyKey, kb.numpadPlusKey, kb.numpadMinusKey };
                    for (int i = 0; i < keys.Length; i++) if (keys[i].isPressed) view = i;
                }
                bool free = view == 6 || view == 7 || view == 25 || view == 26;   // v75e: 26, the pedestrian close up, is a free camera that follows them
                if (view != shown)
                {
                    shown = view; GameClock.FrozenHour = hours[view]; drawLog = Time.time + 1.5f;
                    // v75g: the pedestrian close ups (26, 27) show only the gray tee man: the other pedestrians are hidden
                    {
                        bool solo = view == 25 || view == 26; var keep = solo ? HoodiePed() : null;
                        foreach (var q in FindObjectsByType<StreetEmpire.Game.Ped>(FindObjectsSortMode.None))
                            foreach (var r in q.GetComponentsInChildren<Renderer>(true)) r.enabled = !solo || q == keep;
                    }
                    if (brain) brain.enabled = !free;
                    switch (view)
                    {
                        case 0: case 1: case 8: case 13: Game(b10 + new Vector3(28f, 0f, -20f), Vector3.forward, 8f, 5.2f); break;
                        // v75c: the 22:00 street wide (Backspace): the same street as 1 and 2, the camera higher and
                        // farther back so the run of lamp pools down the road is in frame
                        case 12: case 17: Game(b10 + new Vector3(28f, 0f, -24f), Vector3.forward, 14f, 8f); break;
                        // the skyline past the freeway wall by day: low pitch toward the horizon
                        case 14: Game(b10 + new Vector3(28f, 0f, -20f), Vector3.forward, 3f, 5.2f); break;
                        case 18: { var f = Face("store-pawn", 7f); Game(f.face + f.n * 4.6f + f.t * 1.5f, -f.n, 22f, 6f); break; }
                        case 19:
                        {
                            StreetEmpire.City.BuildingMarker g = null;
                            foreach (var m in FindObjectsByType<StreetEmpire.City.BuildingMarker>(FindObjectsSortMode.None)) if (m.graffiti == 3 && (g == null || (m.graffitiAt - b10).sqrMagnitude < (g.graffitiAt - b10).sqrMagnitude)) g = m;
                            if (g == null) foreach (var m in FindObjectsByType<StreetEmpire.City.BuildingMarker>(FindObjectsSortMode.None)) if (m.graffiti >= 0) { g = m; break; }
                            if (g != null) { var at = g.graffitiAt; at.y = 0; Game(at + g.graffitiNormal * 4.2f, -g.graffitiNormal, 6f, 5.5f); Debug.Log($"[ShotDirector] v75 view 20 graffiti {g.graffiti} on {g.model} at {g.graffitiAt}"); }
                            else Debug.Log("[ShotDirector] v75 view 20: no graffiti placed");
                            break;
                        }
                        case 25: case 26:
                        {
                            // v75f: the gray hoodie pedestrian (ped-m-1), front (26) and side (27); the player is
                            // put 6 m ahead and to the side, out of frame, and the free camera follows them
                            var pd = HoodiePed(); 
                            if (pd != null) { var fw = pd.transform.forward; fw.y = 0; fw.Normalize(); Game(pd.transform.position + fw * 6f + Vector3.Cross(Vector3.up, fw) * 3f, -fw, 4f, 2.4f); Debug.Log($"[ShotDirector] v75 view {view + 1} pedestrian {pd.name} at {pd.transform.position}"); }
                            if (brain) brain.enabled = false;   // Game() switches the follow camera back on
                            break;
                        }
                        case 20:
                        {
                            Transform car = null; float cd = float.MaxValue;
                            foreach (var m in FindObjectsByType<StreetEmpire.City.PropMarker>(FindObjectsSortMode.None))
                                if (m.kind == "parked car" && m.model == "sedan") { float d = (m.transform.position - b10).sqrMagnitude; if (d < cd) { cd = d; car = m.transform; } }
                            if (car != null) { var side = car.right; side.y = 0; side.Normalize(); Game(car.position + side * 3.2f + car.forward * 0.8f, -side, 12f, 4.6f); Debug.Log($"[ShotDirector] v75 view 21 sedan at {car.position}"); }
                            break;
                        }
                        case 2: case 16:
                        {
                            // v75b: the lamp on this apartment's own curb (in front of the wall, beside it), the player
                            // just inside it toward the middle of the wall, the game camera behind them over the street:
                            // lamp, pool, player and the apartment all in frame
                            var f = Face("apt-01", 12f); Vector3 lp = f.face + f.n * 7f + f.t * 6f; float bl = float.MaxValue;
                            foreach (var m in FindObjectsByType<StreetEmpire.City.PropMarker>(FindObjectsSortMode.None))
                            {
                                if (m.model != "street-lamp-1") continue;
                                var o = m.transform.position - f.face; float ahead = Vector3.Dot(o, f.n), side = Vector3.Dot(o, f.t);
                                // v75c: looser (v75b's 3 to 13 m out and 14 m along found none): any lamp in front of
                                // the wall out to the far curb and up to 30 m along it, the nearest along the wall wins
                                if (ahead < 0.5f || ahead > 18f || Mathf.Abs(side) > 30f) continue;
                                if (Mathf.Abs(side) < bl) { bl = Mathf.Abs(side); lp = m.transform.position; }
                            }
                            float sd = Mathf.Sign(Vector3.Dot(lp - f.face, f.t)); if (sd == 0) sd = 1;
                            var spot = lp - f.n * 1.4f - f.t * sd * 3f;
                            Debug.Log($"[ShotDirector] v75 view 3 lamp at {lp}, {bl:0.0} m along the wall");
                            var aim = Vector3.Lerp(f.face, new Vector3(lp.x, f.face.y, lp.z), bl > 8f ? 0.4f : 0f) + Vector3.up * 3f;
                            Game(spot, aim - spot, 16f, bl > 8f ? 8.5f : 7f); break;
                        }
                        case 3: case 21:
                        {
                            // v75e: slide along the front to the spot where no pole, lamp, palm or signal stands
                            // between the camera (about 9 m out) and the sign, then frame the shop from there
                            var f = Face("store-pawn", 7f); float bestS = 0.6f, bestClear = -1f;
                            for (float sl = -4f; sl <= 4.01f; sl += 0.5f)
                            {
                                var camAt = f.face + f.n * 9.2f + f.t * sl; var sign = f.face; float clear = 99f;
                                foreach (var q in blockers)
                                {
                                    var qq = q; qq.y = 0; var a = camAt; a.y = 0; var b = sign; b.y = 0; var seg = b - a;
                                    float u = Mathf.Clamp01(Vector3.Dot(qq - a, seg) / seg.sqrMagnitude);
                                    clear = Mathf.Min(clear, (a + seg * u - qq).magnitude);
                                }
                                clear -= Mathf.Abs(sl) * 0.05f;
                                if (clear > bestClear) { bestClear = clear; bestS = sl; }
                            }
                            Debug.Log($"[ShotDirector] v75 pawn view slide {bestS:0.0} m, clear {bestClear:0.0} m");
                            Game(f.face + f.n * 4.2f + f.t * bestS, -f.n, 6f, 5f); break;
                        }
                        // v75e: the whole apartment with its roof: from farther out, a lower and longer camera
                        case 4: case 22:
                        {
                            // v75g: a set spot straight across from the apartment's front: the camera about 19 m out
                            // on the middle of the facade. Its view wedge (camera to both front corners, 1 m wider
                            // each side) must hold no palm, tree, pole, lamp or signal; if the middle is blocked it
                            // steps out along the front, then farther back, the nearest clear spot wins.
                            var f = Face("apt-01", 16f); const float half = 6.5f;
                            float bestS = 0f, bestBack = 19f; int bestN = int.MaxValue; float bestCost = float.MaxValue;
                            foreach (float back in new[] { 19f, 17f, 21.5f })
                                for (float sl = 0f; sl <= 6.01f; sl += 0.5f)
                                    foreach (float sg in new[] { 1f, -1f })
                                    {
                                        float slide = sl * sg; var camAt = f.face + f.n * back + f.t * slide; camAt.y = 0;
                                        var L = f.face + f.t * half; var R = f.face - f.t * half; L.y = R.y = 0;
                                        int nIn = 0;
                                        foreach (var q in blockers)
                                        {
                                            var qq = q; qq.y = 0;
                                            float along = Vector3.Dot(qq - camAt, -f.n); if (along < 0.5f || along > back + 0.3f) continue;   // behind the lens or behind the wall
                                            float k = along / back; var lft = Vector3.Lerp(camAt, L, k); var rgt = Vector3.Lerp(camAt, R, k);
                                            float tq = Vector3.Dot(qq - camAt, f.t), tl = Vector3.Dot(lft - camAt, f.t), tr = Vector3.Dot(rgt - camAt, f.t);
                                            if (tq < Mathf.Max(tl, tr) + 1f && tq > Mathf.Min(tl, tr) - 1f) nIn++;
                                        }
                                        float cost = nIn * 100f + Mathf.Abs(slide) + Mathf.Abs(back - 19f) * 0.5f;
                                        if (cost < bestCost) { bestCost = cost; bestN = nIn; bestS = slide; bestBack = back; }
                                        if (sl == 0f) break;
                                    }
                            Debug.Log($"[ShotDirector] v75 apartment camera {bestBack:0.0} m out, {bestS:0.0} m along, {bestN} obstacles in view");
                            Game(f.face + f.n * (bestBack - 8.5f) + f.t * bestS, -f.n, 5f, 8.5f); break;
                        }
                        case 5: case 23: case 24: { var f = Face("house-01", 9f); Game(f.face + f.n * 4.5f + f.t * 1.2f, -f.n, 8f, 5f); break; }
                        // v75b: the liquor store sign at 22:00, and the mixed use block (the v74 black tower) by day
                        case 10: { var f = Face("store-liquor", 8f); Game(f.face + f.n * 6f - f.t * 2.5f, -f.n + f.t * 0.25f, 10f, 5.5f); break; }   // v75c: off the utility pole
                        case 11: { var f = Face("apt-mixed-use", 12f); Game(f.face + f.n * 8f + f.t * 2f, -f.n, 14f, 6.5f); break; }
                        case 9: case 15:
                        {
                            // the signal crossing at the south east corner of block 10: standing on the crosswalk
                            // looking at one corner, its pole on the sidewalk and its arm over the lanes
                            var x = b10 + new Vector3(29f, 0, -29f); Transform sig = null; float bd = float.MaxValue;
                            foreach (var m in FindObjectsByType<StreetEmpire.City.PropMarker>(FindObjectsSortMode.None))
                                if (m.kind == "traffic signal") { float d = (PoleOf(m.transform) - (b10 + new Vector3(21.75f, 0, -20.5f))).sqrMagnitude; if (d < bd) { bd = d; sig = m.transform; } }
                            var pole = sig ? PoleOf(sig) : x; pole.y = 0;
                            var from = x + (x - pole).normalized * 2f;
                            if (view == 15) { from = pole + (from - pole).normalized * 8f; Game(from, pole - from, 0f, 4f); }   // v75c: closer and level, the heads in the upper frame
                            else Game(from, pole - from, 10f, 6f);
                            Debug.Log($"[ShotDirector] v75 signal pole at {pole} (crossing {x})"); break;
                        }
                    }
                    var ll = FindFirstObjectByType<StreetEmpire.City.LampLights>(); int on = 0; if (ll) foreach (var l in ll.lights) if (l && l.enabled) on++;
                    var sun = RenderSettings.sun;
                    Debug.Log($"[ShotDirector] v75 view {view + 1} at {hours[view]}, lamp lights on {on}, sun {(sun && sun.enabled ? sun.intensity.ToString("0.00") : "off")}");
                }
                if (Time.time > drawLog) { drawLog = float.MaxValue; Debug.Log($"[ShotDirector] v75 view {view + 1} draws {draws.LastValue} batches {batches.LastValue} setpass {setpass.LastValue}"); }
                if (free)
                {
                    (Vector3 pos, Vector3 at) v;
                    if (view == 25 || view == 26)
                    {
                        var pd = HoodiePed();
                        var a = pd ? pd.transform.position : b10; var fw = pd ? pd.transform.forward : Vector3.forward; fw.y = 0; fw.Normalize();
                        var side = Vector3.Cross(Vector3.up, fw);
                        var off = view == 25 ? fw * 2.3f + side * 0.3f : side * 2.3f + fw * 0.2f;
                        v = (a + off + Vector3.up * 1.3f, a + Vector3.up * 1.05f);
                    }
                    else if (view == 6) { var palm = Prop("palm-short", b10 + new Vector3(0, 0, -27f)); var a = palm ? palm.position : b10; v = (a + new Vector3(3.5f, 4.6f, -3.5f), a + new Vector3(0, 7.2f, 0)); }
                    else { var c = (b0 + b20) / 2; v = (c + new Vector3(60f, 38f, -110f), c + new Vector3(-40f, 0f, 120f)); cam.farClipPlane = 800f; }
                    cam.transform.position = v.pos; cam.transform.LookAt(v.at);
                }
                yield return null;
            }
        }

        IEnumerator V74(PlayerMotor p)
        {
            var cam = Camera.main;
            var brain = cam.GetComponent<Unity.Cinemachine.CinemachineBrain>(); if (brain) brain.enabled = false;
            Vector3 Block(int id) { foreach (var m in FindObjectsByType<StreetEmpire.City.BlockMarker>(FindObjectsSortMode.None)) if (m.blockId == id) return m.transform.position; return Vector3.zero; }
            var b10 = Block(10); var b0 = Block(0); var b20 = Block(20);
            Transform Prop(string model, Vector3 near)
            {
                Transform best = null; float bd = float.MaxValue;
                foreach (var m in FindObjectsByType<StreetEmpire.City.PropMarker>(FindObjectsSortMode.None))
                    if (m.model == model) { float d = (m.transform.position - near).sqrMagnitude; if (d < bd) { bd = d; best = m.transform; } }
                return best;
            }
            // the street front of one building of the model: its foot, outward normal, tangent and height. The front is
            // the holder's forward or back, whichever points away from its block; of every copy of the model, the one
            // with the fewest palms, poles and lamps between the camera spot (back m out) and the wall
            var blockers = new System.Collections.Generic.List<Vector3>();
            foreach (var m in FindObjectsByType<StreetEmpire.City.PropMarker>(FindObjectsSortMode.None))
                if (m.model != null && (m.model.StartsWith("palm") || m.model.StartsWith("tree") || m.model.Contains("pole") || m.model.Contains("lamp") || m.model.Contains("signal"))) blockers.Add(m.transform.position);
            (Vector3 face, Vector3 n, Vector3 t, float h) Face(string model, float back = 9f)
            {
                (Vector3, Vector3, Vector3, float) best = (b10, Vector3.back, Vector3.right, 8f); float bestScore = float.MaxValue;
                foreach (var m in FindObjectsByType<StreetEmpire.City.BuildingMarker>(FindObjectsSortMode.None))
                {
                    if (m.model == null || !m.model.StartsWith(model)) continue;
                    var c = m.GetComponent<BoxCollider>(); var bb = c ? c.bounds : new Bounds(m.transform.position, Vector3.one * 10);
                    var o = bb.center - Block(m.blockId); o.y = 0;
                    var n = m.transform.forward; n.y = 0; n.Normalize(); if (Vector3.Dot(n, o) < 0) n = -n;
                    var face = bb.center + n * Mathf.Abs(Vector3.Dot(bb.extents, new Vector3(Mathf.Abs(n.x), 0, Mathf.Abs(n.z)))); face.y = bb.min.y;
                    var cam0 = face + n * back; float score = 0;
                    foreach (var q in blockers)
                    {
                        var qq = q; qq.y = face.y; var seg = face - cam0; float u = Mathf.Clamp01(Vector3.Dot(qq - cam0, seg) / seg.sqrMagnitude);
                        if ((cam0 + seg * u - qq).magnitude < 1.6f) score += 1f;
                    }
                    score += (bb.center - b10).magnitude * 0.002f;
                    if (score < bestScore) { bestScore = score; best = (face, n, Vector3.Cross(Vector3.up, n), bb.size.y); }
                }
                return best;
            }
            float[] hours = { 14f, 19f, 22f, 22f, 22f, 14f, 14f, 14f, 14f };
            int view = 0; int shown = -1;
            p.Teleport(b10 + new Vector3(28f, 0.4f, -20f), 0f);
            while (true)
            {
                var kb = UnityEngine.InputSystem.Keyboard.current;
                if (kb != null)
                {
                    var keys = new[] { kb.digit1Key, kb.digit2Key, kb.digit3Key, kb.digit4Key, kb.digit5Key, kb.digit6Key, kb.digit7Key, kb.digit8Key, kb.digit9Key };
                    for (int i = 0; i < keys.Length; i++) if (keys[i].isPressed) view = i;
                }
                if (view != shown)
                {
                    shown = view; GameClock.FrozenHour = hours[view];
                    var ll = FindFirstObjectByType<StreetEmpire.City.LampLights>(); int on = 0; if (ll) foreach (var l in ll.lights) if (l && l.enabled) on++;
                    Debug.Log($"[ShotDirector] v74 view {view + 1} at {hours[view]}, lamp lights on {on}");
                }
                (Vector3 pos, Vector3 at) v;
                switch (view)
                {
                    case 0: case 1: v = (b10 + new Vector3(28f, 1.7f, -24f), b10 + new Vector3(27f, 1.5f, 30f)); break;
                    case 2:
                    {
                        var f = Face("apt-01", 12f); var lamp = Prop("street-lamp-1", f.face + f.n * 6f); var lp = lamp ? lamp.position : f.face + f.n * 6f;
                        var cpos = lp + f.n * 7f + f.t * 5f; cpos.y = 1.8f;
                        v = (cpos, Vector3.Lerp(lp, f.face, 0.5f) + Vector3.up * 3.2f); break;
                    }
                    case 3: { var f = Face("store-pawn", 7f); v = (f.face + f.n * 7f + f.t * 1.2f + Vector3.up * 1.7f, f.face + Vector3.up * 2.2f); break; }
                    case 4: { var f = Face("apt-01", 12f); v = (f.face + f.n * 12f + f.t * 2.5f + Vector3.up * 1.7f, f.face + Vector3.up * 4.5f); break; }
                    case 5: { var f = Face("house-02", 9f); v = (f.face + f.n * 9f + f.t * 2f + Vector3.up * 1.7f, f.face + Vector3.up * 2.8f); break; }
                    case 6: { var palm = Prop("palm-short", b10 + new Vector3(0, 0, -27f)); var a = palm ? palm.position : b10; v = (a + new Vector3(3.5f, 4.6f, -3.5f), a + new Vector3(0, 7.2f, 0)); break; }
                    case 7: { var c = (b0 + b20) / 2; v = (c + new Vector3(60f, 38f, -110f), c + new Vector3(-40f, 0f, 120f)); cam.farClipPlane = 800f; break; }
                    default: { var f = Prop("wood-fence", b10 + new Vector3(0, 0, -27f)); var a = f ? f.position : b10; var fw = f ? f.forward : Vector3.forward; v = (a + fw * 2.6f + (f ? f.right : Vector3.right) * 0.8f + Vector3.up * 1.4f, a + Vector3.up * 0.9f); break; }
                }
                cam.transform.position = v.pos; cam.transform.LookAt(v.at);
                yield return null;
            }
        }

        /// <summary>Triangles of what is drawn, once each, by kind: the counter above adds every pass (depth
        /// prepass for SSAO, each shadow cascade), so this shows where they come from.</summary>
        static string Breakdown()
        {
            long build = 0, chars = 0, other = 0;
            foreach (var r in FindObjectsByType<Renderer>(FindObjectsSortMode.None))
            {
                if (!r.isVisible || !r.enabled) continue;
                Mesh m = r is SkinnedMeshRenderer sk ? sk.sharedMesh : r.GetComponent<MeshFilter>()?.sharedMesh;
                if (m == null) continue;
                long t = 0; for (int i = 0; i < m.subMeshCount; i++) t += m.GetIndexCount(i) / 3;
                if (r is SkinnedMeshRenderer) chars += t;
                else if (r.GetComponentInParent<StreetEmpire.City.BuildingMarker>() != null) build += t;
                else other += t;
            }
            var pi = FindFirstObjectByType<StreetEmpire.City.PropInstancer>();
            return $"buildings {build} (incl. shadow proxies), characters {chars}, other renderers {other}, instanced props {(pi ? pi.LastTriangles : 0)} in {(pi ? pi.LastInstances : 0)} copies";
        }

        /// <summary>Upper body lean of a character from hips to neck, degrees, positive forward.</summary>
        public static float Lean(Animator a, Transform root)
        {
            if (a == null || !a.isHuman || a.GetBoneTransform(HumanBodyBones.Neck) == null) return 0;
            var v = Vector3.ProjectOnPlane(a.GetBoneTransform(HumanBodyBones.Neck).position - a.GetBoneTransform(HumanBodyBones.Hips).position, root.right);
            return Vector3.SignedAngle(Vector3.up, v, root.right);
        }

        /// <summary>Angle at the knee (upper leg, knee, ankle), degrees; 180 is straight.</summary>
        public static float Knee(Animator a, bool left)
        {
            if (a == null || !a.isHuman) return 0;
            var up = a.GetBoneTransform(left ? HumanBodyBones.LeftUpperLeg : HumanBodyBones.RightUpperLeg);
            var kn = a.GetBoneTransform(left ? HumanBodyBones.LeftLowerLeg : HumanBodyBones.RightLowerLeg);
            var ft = a.GetBoneTransform(left ? HumanBodyBones.LeftFoot : HumanBodyBones.RightFoot);
            if (up == null || kn == null || ft == null) return 0;
            return Vector3.Angle(up.position - kn.position, ft.position - kn.position);
        }

        IEnumerator PlayerLegs(PlayerMotor motor)
        {
            var cam = Camera.main;
            var brain = cam.GetComponent<Unity.Cinemachine.CinemachineBrain>(); if (brain) brain.enabled = false;
            var anim = motor.animator;
            // the player starts in the street at the east edge of the block: film from further out in the street
            var side = Vector3.right;
            int phase = 1; float nextLog = 0;
            while (true)
            {
                var kb = UnityEngine.InputSystem.Keyboard.current;
                if (kb != null)
                {
                    if (kb.digit1Key.isPressed) phase = 1;
                    if (kb.digit2Key.isPressed) phase = 2;
                    if (kb.digit3Key.isPressed) phase = 3;
                }
                motor.scriptMove = phase == 1 ? Vector2.zero : phase == 2 ? new Vector2(0.2f, 0) : new Vector2(1f, 0);
                motor.scriptSprint = phase == 3;
                var tr = motor.transform;
                cam.transform.position = tr.position + side * 3.6f + Vector3.up * 1.0f;
                cam.transform.LookAt(tr.position + Vector3.up * 0.85f);
                if (Time.time > nextLog)
                {
                    nextLog = Time.time + 0.5f;
                    Debug.Log($"[ShotDirector] legs {(phase == 1 ? "idle" : phase == 2 ? "walk" : "sprint")} speed {motor.PlanarSpeed:0.0} knees {Knee(anim, true):0} {Knee(anim, false):0} lean {Lean(anim, motor.transform):0.0}");
                }
                yield return null;
            }
        }

        IEnumerator FollowPed()
        {
            var cam = Camera.main;
            var brain = cam.GetComponent<Unity.Cinemachine.CinemachineBrain>(); if (brain) brain.enabled = false;
            var peds = FindObjectsByType<Ped>(FindObjectsSortMode.InstanceID);
            if (peds.Length == 0) yield break;
            var ped = peds[0];
            float t = 0, nextLog = 1;
            while (t < 60f)
            {
                t += Time.deltaTime;
                var tr = ped.transform;
                // from the street side of the sidewalk (never from inside the building next to it)
                var d = tr.position - ped.blockCenter; d.y = 0;
                var outward = Mathf.Abs(d.x) > Mathf.Abs(d.z) ? new Vector3(Mathf.Sign(d.x), 0, 0) : new Vector3(0, 0, Mathf.Sign(d.z));
                cam.transform.position = tr.position + outward * 3.8f + Vector3.up * 1.1f;
                cam.transform.LookAt(tr.position + Vector3.up * 0.9f);
                if (t > nextLog)
                {
                    nextLog += 1;
                    var b = ped.GetComponent<NpcBody>();
                    Debug.Log($"[ShotDirector] ped {ped.name} lean {b.Lean:0.0} speed {b.Speed:0.00} y {tr.position.y:0.00}");
                }
                yield return null;
            }
        }
    }
}
