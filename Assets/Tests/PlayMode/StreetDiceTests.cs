// Street dice in the scene: the home corner has the game laid out, standing at it offers the bet, a bet is thrown
// and settled against the pot, the dice land showing the throw, and walking off a live bet loses it.
using System.Collections;
using System.Linq;
using NUnit.Framework;
using StreetEmpire.Game;
using StreetEmpire.Player;
using StreetEmpire.Sim;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace StreetEmpire.Tests
{
    [PrebuildSetup(typeof(SceneSetup))]
    public class StreetDiceTests
    {
        [TearDown] public void Reset() { StreetDice.AutoStart = false; JobBoard.AutoStart = true; SideHustles.AutoStart = true; MuggerOpening.AutoStart = true; Time.timeScale = 1f; }

        [UnityTest, Timeout(300000)]
        public IEnumerator TheHomeCornerShootsDice()
        {
            Time.timeScale = 1f; MuggerOpening.AutoStart = false; JobBoard.AutoStart = false; SideHustles.AutoStart = false; StreetDice.AutoStart = true;
            SceneManager.LoadScene("Eastside");
            yield return null; yield return null;
            var s = GameSession.Instance; var player = Object.FindFirstObjectByType<PlayerMotor>(); var dice = StreetDice.Instance; var turf = TurfWar.Instance;
            Assert.IsNotNull(dice, "street dice"); Assert.IsNotNull(dice.template, "the props template");
            Assert.IsFalse(dice.template.activeSelf, "the template stays out of the game");
            Assert.AreEqual(2 * 21 + 2 + 1 + 3 + 1, dice.template.GetComponentsInChildren<MeshRenderer>(true).Length, "a ring, two dice with 21 pips each, three bills and the backstop");
            Assert.IsTrue(dice.template.GetComponentsInChildren<Collider>(true).Length == 0, "nothing to trip over");
            Assert.IsTrue(dice.Spot(0, out var spot), "the home block has a corner for it");
            Assert.Less(Vector2.Distance(new Vector2(spot.x, spot.z), new Vector2(turf.Center(0).x, turf.Center(0).z)), 30f, "on the home block");

            // away from the corner: no game, the buy prompt is the turf's own
            Assert.AreEqual(-1, dice.Table); Assert.IsFalse(dice.Near);

            // stand at it: the props are laid out, the prompt offers the bet with the pot
            // out from the block's middle: the side of the corner that is sidewalk and street, never a building
            Vector3 outward = spot - turf.Center(0); outward.y = 0f; outward.Normalize();
            player.Teleport(spot + outward * 0.9f + Vector3.up * 0.3f, 180f);
            yield return new WaitForSeconds(0.8f);
            Assert.AreEqual(0, dice.Table, "standing at the home corner's game");
            var game = dice.Games.FirstOrDefault(); Assert.IsNotNull(game, "the game is laid out"); Assert.AreEqual("Dice Game 0", game.name);
            Assert.Less(Vector3.Distance(game.transform.position, spot), 0.05f);
            float cap = Dice.PotCap(turf.world, turf.State.Block(0));
            StringAssert.Contains("Shoot dice ($10)", dice.Prompt); StringAssert.Contains($"Pot ${Mathf.FloorToInt(cap)}", dice.Prompt);
            Assert.IsTrue(turf.prompt.gameObject.activeSelf, "the prompt line shows it"); StringAssert.Contains("Shoot dice", turf.prompt.text);
            var panel = GameObject.Find("Dice Bets"); Assert.IsNotNull(panel, "the bet buttons");
            Assert.AreEqual(4, panel.GetComponentsInChildren<UnityEngine.UI.Button>(false).Length, "four bets while nothing is down");

            // shoot until three bets have settled: every one moves the bet between the player and the pot
            int settled = 0, guard = 0;
            while (settled < 3 && guard++ < 400)
            {
                float cash0 = s.cash, pot0 = turf.State.Block(0).pot; bool fresh = dice.Live == null; int n0 = dice.Throws;
                dice.Tap(fresh ? 5 : 0);
                yield return null; yield return null;
                Assert.AreEqual(n0 + 1, dice.Throws, "one tap, one throw");
                var t = dice.Last; Assert.AreEqual(5, t.bet); Assert.AreEqual(t.a + t.b, t.sum);
                // the stash on the block pays in while he stands there, so cash is checked from below with a little room above
                float want = (fresh ? -5f : 0f) + t.pay;
                Assert.That(s.cash - cash0, Is.InRange(want - 0.01f, want + 6f), $"cash after {t.result}");
                if (t.result == "win") { Assert.AreEqual(10, t.pay); Assert.That(turf.State.Block(0).pot, Is.InRange(pot0 - 5.01f, pot0 - 4.5f)); }
                if (t.result == "lose") Assert.That(turf.State.Block(0).pot, Is.InRange(pot0 + 4.99f, pot0 + 5.5f));
                if (t.result == "win" || t.result == "lose") { settled++; Assert.IsNull(dice.Live); Assert.AreEqual(t.result, dice.LastEnd); }
                else
                {
                    Assert.IsNotNull(dice.Live); Assert.AreEqual(t.point, dice.Live.point); StringAssert.Contains($"Point {t.point}", dice.Prompt);
                    Assert.AreEqual(1, panel.GetComponentsInChildren<UnityEngine.UI.Button>(false).Length, "one ROLL button with a point on");
                }
                StringAssert.Contains($"{t.a} and {t.b}", s.Message);
            }
            Assert.AreEqual(3, settled, "three bets settled");

            // the dice come to rest inside the ring showing the last throw
            yield return new WaitForSeconds(0.8f);
            var last = dice.Last; int[] show = { last.a, last.b };
            for (int i = 0; i < 2; i++)
            {
                var die = game.transform.Find($"Die {i + 1}"); Assert.IsNotNull(die);
                Assert.Greater(Vector3.Dot(die.TransformDirection(StreetDice.Face(show[i])), Vector3.up), 0.98f, $"die {i + 1} shows {show[i]}");
                Assert.Less(new Vector2(die.localPosition.x, die.localPosition.z).magnitude, 0.75f, "inside the ring");
            }

            // a point on, then walking off: the bet is gone and the pot has it
            guard = 0;
            while ((dice.Live == null || dice.Live.point == 0) && guard++ < 200) { dice.Tap(dice.Live == null ? 5 : 0); yield return null; yield return null; }
            Assert.IsNotNull(dice.Live, "a point is on");
            float potLive = turf.State.Block(0).pot, cashLive = s.cash;
            player.Teleport(spot + outward * (Dice.Leave + 3f) + Vector3.up * 0.3f, 0f);
            yield return new WaitForSeconds(0.3f);
            Assert.IsNull(dice.Live, "walking off ends it"); Assert.AreEqual("forfeit", dice.LastEnd);
            Assert.That(turf.State.Block(0).pot, Is.InRange(potLive + 4.99f, potLive + 5.5f), "the pot keeps the bet");
            Assert.That(s.cash - cashLive, Is.InRange(-0.01f, 6f), "nothing comes back");
            Assert.AreEqual(-1, dice.Table); Assert.IsFalse(dice.Near);

            // the other corners: a card table on block 1, a boombox on block 2, both props only
            var scenes = CornerScenes.Instance; Assert.IsNotNull(scenes, "corner scenes");
            Assert.AreEqual(20, scenes.cards.GetComponentsInChildren<MeshRenderer>(true).Length, "a table, four legs, six cards, seven chips and two crates");
            Assert.AreEqual(7, scenes.stoop.GetComponentsInChildren<MeshRenderer>(true).Length, "a crate, the box with two speakers and a handle, a cooler and its lid");
            Assert.AreEqual(0, scenes.cards.GetComponentsInChildren<Collider>(true).Length + scenes.stoop.GetComponentsInChildren<Collider>(true).Length, "nothing to trip over");
            Assert.AreEqual("dice", CornerScenes.Kind(9)); Assert.AreEqual("cards", CornerScenes.Kind(1)); Assert.AreEqual("stoop", CornerScenes.Kind(2));
            foreach (var (blk, name) in new[] { (1, "Card Game 1"), (2, "Stoop 2") })
            {
                Assert.IsTrue(dice.Spot(blk, out var at), $"block {blk} has a corner");
                Vector3 away = at - turf.Center(blk); away.y = 0f; away.Normalize();
                player.Teleport(at + away * 2.5f + Vector3.up * 0.3f, 0f);
                yield return new WaitForSeconds(0.8f);
                var go = GameObject.Find(name); Assert.IsNotNull(go, name);
                Assert.Less(Vector3.Distance(go.transform.position, at), 0.05f);
                Assert.AreEqual(-1, dice.Table, "no bets at a card table or a boombox");
            }
            var cone = GameObject.Find("Stoop 2").transform.Find("Boombox/Speaker"); Assert.IsNotNull(cone, "a speaker");
            float lo = 9f, hi = 0f;
            for (int f = 0; f < 60; f++) { lo = Mathf.Min(lo, cone.localScale.x); hi = Mathf.Max(hi, cone.localScale.x); yield return null; }
            Assert.Greater(hi - lo, 0.005f, "the speakers thump");
        }
    }
}
