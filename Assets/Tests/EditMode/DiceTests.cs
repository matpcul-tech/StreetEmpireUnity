// Street dice rules (Assets/Scripts/Sim/Dice.cs): which corners run it, the pot, a natural, craps, a point made and
// lost, the bets that are refused, a walked-off bet and the refill.
using System.IO;
using NUnit.Framework;
using StreetEmpire.Sim;
using StreetEmpire.Sim.Data;
using UnityEngine;

namespace StreetEmpire.Tests
{
    public class DiceTests
    {
        static WorldFile w;

        [OneTimeSetUp]
        public void Load()
        {
            string R(string n) => File.ReadAllText(Path.Combine(Application.dataPath, "Data", n + ".json"));
            w = GameDataLoader.FromJson(R("crews"), R("weapons"), R("vehicles"), R("jobs"), R("police"), R("world")).World;
        }

        [Test]
        public void TheHomeCornerRunsAGameWithAFullPot()
        {
            var s = Turf.CreateEastside(w, 1); var b = s.Block(0);
            Assert.IsTrue(Dice.Runs(b), "block 0 is a dice corner"); Assert.IsFalse(Dice.Runs(s.Block(1))); Assert.IsTrue(Dice.Runs(s.Block(9)));
            Assert.AreEqual(40f + b.rate * 10f, Dice.PotCap(w, b));
            Assert.AreEqual(Dice.PotCap(w, b), Dice.Pot(w, b), "full the first time");
            b.built = true; Assert.AreEqual((40f + b.rate * 10f) * w.buildBoost, Dice.PotCap(w, b), "a built block's pot is bigger");
        }

        [Test]
        public void NaturalsCrapsAndPoints()
        {
            var s = Turf.CreateEastside(w, 1); var b = s.Block(0); var you = s.Crew(Turf.You);
            float cap = Dice.PotCap(w, b);

            // 7 on the first roll: even money out of the pot
            var g = Dice.Open(s, w, b, 10); Assert.IsNotNull(g); Assert.AreEqual(140f, you.cash, "the bet is down");
            var t = Dice.Settle(s, w, g, 3, 4);
            Assert.AreEqual("win", t.result); Assert.AreEqual(20, t.pay); Assert.AreEqual(160f, you.cash); Assert.AreEqual(cap - 10f, b.pot);
            // 11 wins too
            g = Dice.Open(s, w, b, 5); Assert.AreEqual("win", Dice.Settle(s, w, g, 5, 6).result); Assert.AreEqual(165f, you.cash);

            // 2, 3 and 12 lose on the first roll, and the pot keeps the bet
            foreach (var (a, c) in new[] { (1, 1), (1, 2), (6, 6) })
            {
                float cash = you.cash, pot = b.pot;
                g = Dice.Open(s, w, b, 5); t = Dice.Settle(s, w, g, a, c);
                Assert.AreEqual("lose", t.result, $"{a + c}"); Assert.AreEqual(0, t.pay); Assert.AreEqual(cash - 5f, you.cash); Assert.AreEqual(pot + 5f, b.pot);
            }

            // a point: rolls that are neither keep it going, the point back wins
            float before = you.cash;
            g = Dice.Open(s, w, b, 25); t = Dice.Settle(s, w, g, 4, 4);
            Assert.AreEqual("point", t.result); Assert.AreEqual(8, g.point); Assert.AreEqual(before - 25f, you.cash);
            Assert.AreEqual("roll", Dice.Settle(s, w, g, 5, 6).result, "11 means nothing with a point on");
            Assert.AreEqual("roll", Dice.Settle(s, w, g, 1, 1).result);
            t = Dice.Settle(s, w, g, 6, 2); Assert.AreEqual("win", t.result); Assert.AreEqual(before + 25f, you.cash); Assert.AreEqual(4, g.n);
            // and a seven before it loses
            before = you.cash;
            g = Dice.Open(s, w, b, 10); Dice.Settle(s, w, g, 2, 2);
            Assert.AreEqual("lose", Dice.Settle(s, w, g, 3, 4).result); Assert.AreEqual(before - 10f, you.cash);
        }

        [Test]
        public void BetsThatAreRefused()
        {
            var s = Turf.CreateEastside(w, 1); var you = s.Crew(Turf.You);
            Assert.AreNotEqual("", Dice.Refuse(s, w, s.Block(1), 10), "not a dice corner");
            Assert.AreNotEqual("", Dice.Refuse(s, w, s.Block(18), 10), "not your block");
            Assert.AreNotEqual("", Dice.Refuse(s, w, s.Block(0), 7), "not a bet on the table");
            Assert.AreEqual("", Dice.Refuse(s, w, s.Block(0), 50));
            you.cash = 20f; Assert.AreEqual("You need $25 to get in.", Dice.Refuse(s, w, s.Block(0), 25)); Assert.IsNull(Dice.Open(s, w, s.Block(0), 25));
            you.cash = 500f; s.Block(0).hasPot = true; s.Block(0).pot = 8f;
            StringAssert.Contains("cannot cover $10", Dice.Refuse(s, w, s.Block(0), 10)); Assert.AreEqual("", Dice.Refuse(s, w, s.Block(0), 5));
        }

        [Test]
        public void TheThrowsAreFairDiceAndThePotRefills()
        {
            var s = Turf.CreateEastside(w, 7); var b = s.Block(0); s.Crew(Turf.You).cash = 100000f;
            var seen = new int[13];
            for (int i = 0; i < 600; i++)
            {
                b.hasPot = true; b.pot = Dice.PotCap(w, b);
                var g = Dice.Open(s, w, b, 5); var t = Dice.Roll(s, w, g);
                Assert.That(t.a, Is.InRange(1, 6)); Assert.That(t.b, Is.InRange(1, 6)); Assert.AreEqual(t.a + t.b, t.sum);
                seen[t.sum]++;
            }
            for (int n = 2; n <= 12; n++) Assert.Greater(seen[n], 0, $"a {n} comes up");
            Assert.Greater(seen[7], seen[2]); Assert.Greater(seen[7], seen[12]);

            // a walked-off bet stays in the pot, a pot never holds more than twice its cap, and a played pot comes back
            float cap = Dice.PotCap(w, b);
            b.pot = cap; var live = Dice.Open(s, w, b, 50); Dice.Settle(s, w, live, 2, 2); Dice.Forfeit(s, w, live);
            Assert.AreEqual(cap + 50f, b.pot);
            b.pot = cap * 2f - 1f; Dice.Forfeit(s, w, live); Assert.AreEqual(cap * 2f, b.pot);
            b.pot = cap - 9f; Dice.Step(s, w, 6f); Assert.AreEqual(cap - 7f, b.pot, 1e-3f, "a dollar every three seconds");
            Dice.Step(s, w, 600f); Assert.AreEqual(cap, b.pot);
            b.pot = cap + 20f; Dice.Step(s, w, 60f); Assert.AreEqual(cap + 20f, b.pot, "winnings in the pot stay");
        }
    }
}
