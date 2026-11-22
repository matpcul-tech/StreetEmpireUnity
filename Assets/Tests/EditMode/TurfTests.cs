// The turf loop rules (Assets/Scripts/Sim/Turf.cs): the Eastside start, the stash, taking a block, buying,
// raids, and long runs with nobody playing.
using System.IO;
using System.Linq;
using NUnit.Framework;
using StreetEmpire.Sim;
using StreetEmpire.Sim.Data;
using UnityEngine;

namespace StreetEmpire.Tests
{
    public class TurfTests
    {
        static WorldFile w;
        const float Dt = 0.1f;

        [OneTimeSetUp]
        public void Load()
        {
            string R(string n) => File.ReadAllText(Path.Combine(Application.dataPath, "Data", n + ".json"));
            w = GameDataLoader.FromJson(R("crews"), R("weapons"), R("vehicles"), R("jobs"), R("police"), R("world")).World;
        }

        static void Run(TurfState s, float seconds, int zone = -1, int near = -1)
        {
            int n = Mathf.RoundToInt(seconds / Dt);
            for (int i = 0; i < n; i++) Turf.Step(s, w, new TurfPresence { zoneBlock = zone, nearBlock = near }, Dt);
        }

        [Test]
        public void EastsideStart()
        {
            var s = Turf.CreateEastside(w, 1);
            Assert.AreEqual(9, s.blocks.Count);
            Assert.AreEqual(1, s.Owned(Turf.You)); Assert.AreEqual(Turf.You, s.Block(0).owner);
            Assert.AreEqual("reds", s.Block(20).owner); Assert.AreEqual("blues", s.Block(2).owner); Assert.AreEqual("golds", s.Block(18).owner);
            Assert.AreEqual(5, s.Owned(Turf.Locals));
            Assert.AreEqual(1, s.Block(0).guards); Assert.AreEqual(2, s.Block(1).guards);
            Assert.AreEqual(150f, s.Crew(Turf.You).cash);
            Assert.AreEqual("Corner Store Deals", s.Block(0).hustle); Assert.AreEqual("Car Wash", s.Block(1).hustle);
            Assert.AreEqual(2, Turf.Neighbors(s, s.Block(0)).Count(), "a corner block has two neighbors");
            Assert.AreEqual(4, Turf.Neighbors(s, s.Block(10)).Count(), "the middle block has four");
        }

        [Test]
        public void StashPilesUpAndIsPocketedInTheRing()
        {
            var s = Turf.CreateEastside(w, 1);
            var b = s.Block(0);
            Assert.AreEqual(3 * 0.6f, Turf.StashRate(w, b), 1e-4f, "rate 3, one guard: 0.6x");
            Run(s, 8f);
            Assert.AreEqual(14.4f, b.stash, 0.3f, "eight seconds of the hustle on the block");
            Assert.AreEqual(150f, s.Crew(Turf.You).cash, "not yours until you collect it");
            Run(s, 0.1f, 0, 0);
            Assert.Less(b.stash, 1f);
            Assert.AreEqual(164.6f, s.Crew(Turf.You).cash, 0.5f);
            Assert.IsTrue(s.events.Any(e => e.type == "stash" && e.block == 0));
        }

        [Test]
        public void TakingABlock()
        {
            var s = Turf.CreateEastside(w, 1);
            var b = s.Block(1);
            Run(s, 3f, 1, 1);
            Assert.AreEqual(0f, b.capT, "guards still on it: no capture");
            Turf.GuardDown(s, 1); Turf.GuardDown(s, 1);
            Assert.AreEqual(0, b.guards);
            Assert.IsTrue(s.events.Any(e => e.type == "cleared" && e.block == 1));
            Run(s, 3f, 1, 1);
            Assert.AreEqual(Turf.You, b.capBy); Assert.AreEqual(3f, b.capT, 0.15f);
            Run(s, 1f);   // stepped out of the ring: it drains at twice the speed
            Assert.AreEqual(1f, b.capT, 0.25f);
            Run(s, 6f, 1, 1);
            Assert.AreEqual(Turf.You, b.owner, "held the ring for captureTime");
            Assert.AreEqual(1, b.guards, "one of yours stays to hold it");
            Assert.AreEqual(1, s.taken); Assert.AreEqual(25, s.Crew(Turf.You).rep);
            Assert.AreEqual(2, s.Owned(Turf.You));
            Assert.IsTrue(s.events.Any(e => e.type == "capture" && e.block == 1 && e.crew == Turf.You && e.from == Turf.Locals));
        }

        [Test]
        public void BuyingOnABlockYouHold()
        {
            var s = Turf.CreateEastside(w, 1);
            var b = s.Block(0); var you = s.Crew(Turf.You);
            Assert.AreEqual("recruit", Turf.NextBuy(b));
            Assert.IsFalse(Turf.Recruit(s, w, 1), "not on a block you do not hold");
            Assert.IsTrue(Turf.Recruit(s, w, 0));
            Assert.AreEqual(2, b.guards); Assert.AreEqual(50f, you.cash);
            Assert.AreEqual("arm", Turf.NextBuy(b));
            Assert.IsFalse(Turf.Arm(s, w, 0), "80 needed, 50 in hand");
            you.cash = 1000;
            Assert.IsTrue(Turf.Arm(s, w, 0)); Assert.IsTrue(b.armed); Assert.IsFalse(Turf.Arm(s, w, 0), "once");
            Assert.AreEqual("recruit", Turf.NextBuy(b));
            Assert.IsTrue(Turf.Recruit(s, w, 0)); Assert.AreEqual(3, b.guards);
            Assert.IsFalse(Turf.Recruit(s, w, 0), "three guards a block");
            Assert.AreEqual("build", Turf.NextBuy(b));
            float before = Turf.StashRate(w, b);
            Assert.IsTrue(Turf.Build(s, w, 0));
            Assert.AreEqual(before * 2f, Turf.StashRate(w, b), 1e-4f, "built up: double");
            Assert.IsNull(Turf.NextBuy(b));
            Assert.AreEqual(1000f - 80 - 100 - 200, you.cash);
            Assert.IsNull(Turf.NextBuy(s.Block(1)));
        }

        [Test]
        public void RaidOnYourBlockThatYouBeat()
        {
            var s = Turf.CreateEastside(w, 1);
            var b = s.Block(0);
            b.hasRaid = true; b.raid = new TurfRaid { crew = "reds", from = 1, n = 2, eta = w.raidGather };
            Run(s, 9f, -1, 0);
            Assert.IsFalse(s.events.Any(e => e.type == "raidGo"), "still gathering");
            Run(s, 1.5f, -1, 0);
            Assert.IsTrue(s.events.Any(e => e.type == "raidGo" && e.block == 0 && e.crew == "reds"));
            Assert.AreEqual(Turf.You, b.owner);
            Turf.RaiderDown(s, 0); Assert.IsTrue(b.hasRaid);
            Turf.RaiderDown(s, 0);
            Assert.IsFalse(b.hasRaid, "both raiders down: the raid is over");
            Assert.AreEqual(1, s.defended); Assert.AreEqual(10, s.Crew(Turf.You).rep);
            Assert.IsTrue(s.events.Any(e => e.type == "raidBeaten" && e.block == 0));
        }

        [Test]
        public void RaidOnAnEmptyBlockTakesIt()
        {
            var s = Turf.CreateEastside(w, 1);
            var b = s.Block(0); b.guards = 0; b.built = true; b.stash = 40;
            b.hasRaid = true; b.raid = new TurfRaid { crew = "reds", from = 1, n = 3, eta = 0 };
            Run(s, 2f, -1, 0);
            Assert.AreEqual(0f, b.capT, "you are standing on it: they cannot hold the ring");
            Run(s, 7f);
            Assert.AreEqual("reds", b.owner);
            Assert.AreEqual(3, b.guards, "the raiders stay as its guards");
            Assert.IsFalse(b.built, "the build up is lost with the block"); Assert.IsFalse(b.hasRaid);
            Assert.AreEqual(1, s.lost); Assert.AreEqual(0, s.Owned(Turf.You));
            Assert.IsTrue(s.events.Any(e => e.type == "capture" && e.crew == "reds" && e.from == Turf.You));
        }

        [Test]
        public void TwentyMinutesWithNobodyPlaying()
        {
            foreach (long seed in new long[] { 1, 2, 3, 7, 42 })
            {
                var s = Turf.CreateEastside(w, seed);
                int rivalTakes = 0; bool raidedInGrace = false;
                for (int i = 0; i < 12000; i++)
                {
                    Turf.Step(s, w, new TurfPresence { zoneBlock = -1, nearBlock = -1 }, Dt);
                    if (s.t < Turf.GraceSeconds - 1f && s.Block(0).hasRaid) raidedInGrace = true;
                    foreach (var e in s.events) if (e.type == "capture" && e.crew != Turf.You) rivalTakes++;
                    s.events.Clear();
                }
                Assert.IsFalse(raidedInGrace, $"seed {seed}: a new player is left alone for the first five minutes");
                Assert.Greater(rivalTakes, 0, $"seed {seed}: the rivals take ground");
                Assert.AreEqual(9, s.blocks.Count);
                foreach (var b in s.blocks)
                {
                    Assert.IsTrue(b.guards >= 0 && b.guards <= Turf.MaxGuards + 1, $"seed {seed}: guards {b.guards} on block {b.id}");
                    Assert.IsNotNull(s.Crew(b.owner), $"seed {seed}: owner {b.owner}");
                }
                foreach (var c in s.crews) Assert.GreaterOrEqual(c.cash, 0f, $"seed {seed}: {c.id} cash");
                Assert.AreEqual("", s.over);
            }
        }

        [Test]
        public void SameSeedSameGame()
        {
            var a = Turf.CreateEastside(w, 5); var b = Turf.CreateEastside(w, 5);
            Run(a, 600f); Run(b, 600f);
            Assert.AreEqual(string.Join(",", a.blocks.Select(x => x.owner + x.guards)), string.Join(",", b.blocks.Select(x => x.owner + x.guards)));
        }

        [Test]
        public void SavesAndLoads()
        {
            var s = Turf.CreateEastside(w, 1);
            Turf.GuardDown(s, 1); Turf.GuardDown(s, 1); Run(s, 7f, 1, 1);
            s.Block(0).hasRaid = true; s.Block(0).raid = new TurfRaid { crew = "blues", from = 1, n = 3, eta = 4 };
            var t = JsonUtility.FromJson<TurfState>(JsonUtility.ToJson(s));
            Assert.AreEqual(9, t.blocks.Count); Assert.AreEqual(Turf.You, t.Block(1).owner);
            Assert.AreEqual(s.rng, t.rng); Assert.AreEqual(s.Crew(Turf.You).cash, t.Crew(Turf.You).cash);
            Assert.IsTrue(t.Block(0).hasRaid); Assert.AreEqual("blues", t.Block(0).raid.crew); Assert.AreEqual(3, t.Block(0).raid.n);
            Assert.AreEqual(s.taken, t.taken);
        }

        [Test]
        public void EveryBlockIsAWin()
        {
            var s = Turf.CreateEastside(w, 1);
            foreach (var b in s.blocks) { b.owner = Turf.You; b.guards = 3; b.armed = true; }
            Run(s, 0.2f);
            Assert.AreEqual("win", s.over);
            Assert.IsTrue(s.events.Any(e => e.type == "win"));
        }
    }
}
