// Parity checks against the JS game. Golden values come from running createState(1) in
// matpcul-tech/StreetEmpire a915d2b (v0.29) with node. If the JS changes, regenerate them.
using System.IO;
using NUnit.Framework;
using StreetEmpire.Sim;
using StreetEmpire.Sim.Data;
using UnityEngine;

namespace StreetEmpire.Tests
{
    public class DataAndSimTests
    {
        static GameData data;

        [OneTimeSetUp]
        public void Load()
        {
            string R(string n) => File.ReadAllText(Path.Combine(Application.dataPath, "Data", n + ".json"));
            data = GameDataLoader.FromJson(R("crews"), R("weapons"), R("vehicles"), R("jobs"), R("police"), R("world"));
        }

        [Test]
        public void DataCountsMatchJs()
        {
            Assert.AreEqual(7, data.Crews.crews.Count);
            Assert.AreEqual(5, data.Weapons.weapons.Count);
            Assert.AreEqual(12, data.Vehicles.vehicles.Count); // 9 CAR_TYPES + 3 BIKE_MODELS
            Assert.AreEqual(19, data.Jobs.missions.Count);
            Assert.AreEqual(4, data.Jobs.contacts.Count);
            Assert.AreEqual(5, data.Police.levels.Count);
            Assert.AreEqual(81, data.World.hustles.Count);
            Assert.AreEqual(9, data.World.grid);
            Assert.AreEqual(26f, data.Weapon("pistol").damage);
            Assert.AreEqual(36f, data.Vehicle("muscle").maxSpeed);
            Assert.AreEqual("Bag Back", data.Jobs.missions[0].title);
        }

        // phone driving: GAS and REV pedals set the throttle and the stick only steers; keys and a pad stick still drive
        [Test]
        public void PedalsSetTheThrottle()
        {
            Assert.AreEqual(1f, StreetEmpire.Vehicles.CarController.Throttle(0f, true, false, true), "gas");
            Assert.AreEqual(-1f, StreetEmpire.Vehicles.CarController.Throttle(0f, false, true, true), "reverse");
            Assert.AreEqual(0f, StreetEmpire.Vehicles.CarController.Throttle(0.8f, false, false, true), "on a phone the stick does not drive");
            Assert.AreEqual(1f, StreetEmpire.Vehicles.CarController.Throttle(-1f, true, false, false), "a pedal beats the stick");
            Assert.AreEqual(0.8f, StreetEmpire.Vehicles.CarController.Throttle(0.8f, false, false, false), "keys and a pad stick still drive");
        }

        [Test]
        public void RngMatchesJs()
        {
            var s = new SimState { rng = SimRng.Seed(1) };
            double[] js = { 0.23645552527159452, 0.3692706737201661, 0.5042420323006809, 0.7048832636792213, 0.05054362863302231 };
            foreach (var v in js) Assert.AreEqual(v, SimRng.Next(s), 0.0);
        }

        [Test]
        public void CreateStateMatchesJs()
        {
            var s = Simulation.CreateState(data, 1);
            Assert.AreEqual(81, s.blocks.Count);
            Assert.AreEqual("player", s.blocks[0].owner);
            Assert.AreEqual("reds", s.blocks[7].owner);
            Assert.AreEqual("blues", s.blocks[63].owner);
            Assert.AreEqual("golds", s.blocks[80].owner);
            Assert.AreEqual(6, s.blocks[1].rate);
            Assert.AreEqual("Dry Dock", s.blocks[80].hustle);
            Assert.AreEqual(150f, s.crews["player"].cash);
            Assert.AreEqual(13.692706737201661f, s.crews["player"].aiT, 1e-4f);
        }

        [Test]
        public void StarsMatchJs()
        {
            Assert.AreEqual(0, Simulation.Stars(9)); Assert.AreEqual(1, Simulation.Stars(10));
            Assert.AreEqual(1, Simulation.Stars(29)); Assert.AreEqual(2, Simulation.Stars(30));
            Assert.AreEqual(5, Simulation.Stars(90)); Assert.AreEqual(5, Simulation.Stars(100));
            Assert.IsTrue(Simulation.Level(data.Police, 70).helicopter);
            Assert.IsTrue(Simulation.Level(data.Police, 95).roadblocks);
        }
    }
}

namespace StreetEmpire.Tests
{
    public class CityLayoutTests
    {
        [Test]
        public void EastsideMatchesJs()
        {
            string R(string n) => File.ReadAllText(Path.Combine(Application.dataPath, "Data", n + ".json"));
            var data = GameDataLoader.FromJson(R("crews"), R("weapons"), R("vehicles"), R("jobs"), R("police"), R("world"));
            var s = Simulation.CreateState(data, 1);
            // from node: buildingsFor(s.blocks[1]) in the JS game
            var b1 = CityLayout.BuildingsFor(data.World, s.blocks[1]);
            Assert.AreEqual("store", b1[0].kind); Assert.AreEqual(-187.5f, b1[0].x); Assert.AreEqual(-245.5f, b1[0].z);
            Assert.AreEqual("apt", b1[3].kind); Assert.AreEqual(-160.5f, b1[3].x); Assert.AreEqual(-218.5f, b1[3].z); Assert.AreEqual(11f, b1[3].h);
            var b10 = CityLayout.BuildingsFor(data.World, s.blocks[10]);
            Assert.AreEqual("house", b10[3].kind);
            Assert.AreEqual(CityLayout.Eastside, CityLayout.DistrictOf(2, 2, 9));
            Assert.AreEqual(CityLayout.Midtown, CityLayout.DistrictOf(3, 3, 9));
            Assert.AreEqual(CityLayout.Harbor, CityLayout.DistrictOf(8, 8, 9));
            Assert.AreEqual(-261f, CityLayout.LineCoord(data.World, 0));
            Assert.AreEqual(-87f, CityLayout.LineCoord(data.World, 3));
        }
    }
}
