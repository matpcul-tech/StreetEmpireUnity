// Plain data shapes for the JSON under Assets/Data. Public lowerCamel fields so both
// UnityEngine.JsonUtility (runtime) and System.Text.Json (tools, tests) read the same files.
// Ported from matpcul-tech/StreetEmpire src/state.js (v0.29, a915d2b). No UnityEngine here.
using System;
using System.Collections.Generic;

namespace StreetEmpire.Sim.Data
{
    [Serializable] public class Point { public float x; public float z; }
    [Serializable] public class PointRy { public float x; public float z; public float ry; }
    [Serializable] public class GridSpot { public string name; public int gx; public int gz; }

    // crews.json
    [Serializable] public class CrewData { public string id; public string name; public string color; public string uiColor; public string shirt; public string pants; public string legacyModel; }
    [Serializable] public class CrewFile { public List<CrewData> crews = new(); }

    // weapons.json
    [Serializable] public class WeaponData { public string id; public float range; public float damage; public float cooldown; public float swing; public float knockback; public bool ranged; }
    [Serializable] public class WeaponFile { public float aimAssist; public List<WeaponData> weapons = new(); }

    // vehicles.json
    [Serializable] public class VehicleData { public string id; public string kind; public float maxSpeed; public float accel; public float hp; public float wheelbase; }
    [Serializable] public class GarageData { public string name; public float x; public float z; }
    [Serializable]
    public class VehicleFile
    {
        public List<VehicleData> vehicles = new();
        public int garageCost; public List<GarageData> garages = new();
        public float burnTime; public float blastRadius; public float wreckClear;
        public List<PointRy> ramps = new();
    }

    // jobs.json
    [Serializable] public class ContactData { public string name; public int gx; public int gz; public string slot; public bool female; public int look; public List<string> pass = new(); public List<string> fail = new(); }
    [Serializable]
    public class MissionData
    {
        public int index; public string title; public string by; public string kind;
        public float time; public int reward; public int rep;
        public List<string> brief = new(); public string intro; public string gift;
        public int need; public int district = -1; public string car; public string handoff;
        public float minHp; public string crew; public int heat; public string who; public bool home; public bool rival;
    }
    [Serializable] public class FareData { public float wait; public float gap; public int basePay; public float perMeter; public int tipMax; }
    [Serializable] public class CourierData { public float wait; public float gap; public int basePay; public float perMeter; public int bonusMax; public float builtMultiplier; }
    [Serializable] public class RaceData { public string name; public Point start; public List<Point> points = new(); public float length; public float gold; public float silver; public float bronze; }
    [Serializable] public class RacePayData { public int gold; public int silver; public int bronze; public int none; }
    [Serializable] public class StreetCallData { public float first; public float cooldown; public float offer; public float fight; public int cap; public int maxNpc; public float minDist; public int strikeCap; public List<string> kinds = new(); }
    [Serializable] public class DiceData { public List<int> bets = new(); public float reach; public float leave; }
    [Serializable]
    public class JobFile
    {
        public List<ContactData> contacts = new(); public List<MissionData> missions = new();
        public FareData fare; public CourierData courier;
        public List<RaceData> races = new(); public RacePayData racePay;
        public StreetCallData streetCalls;
        public int stashPay; public int stashAll; public List<Point> stashes = new();
        public DiceData dice;
    }

    // police.json
    [Serializable]
    public class PoliceLevelData
    {
        public int stars; public float minHeat; public int maxUnits; public float spawnInterval;
        public int officersPerCar; public float officerHp; public float cruiserHp; public string officerWeapon;
        public bool tactical; public bool helicopter; public bool roadblocks;
    }
    [Serializable] public class HelicopterData { public float speed; public float sight; public float fuel; public float cooldownAfterFuelOut; public float cooldownAfterLeave; public int leavesBelowStars; public float orbitRadius; }
    [Serializable] public class HeatDecayData { public float copNearRadius; public float calmAfter; public float calmRate; public float longCalmAfter; public float longCalmRate; public float safehouseRate; }
    [Serializable] public class HeatSourceData { public string id; public float heat; }
    [Serializable]
    public class PoliceFile
    {
        public float maxHeat; public List<PoliceLevelData> levels = new();
        public HelicopterData helicopter; public HeatDecayData decay;
        public float spawnRingMin; public float spawnRingMax;
        public float bustedHeat; public float bailBusted; public float bailWasted; public float downTime; public float clearTime;
        public List<HeatSourceData> heatSources = new();
    }

    // world.json
    [Serializable] public class BackupData { public float range; public float run; public float near; }
    [Serializable] public class ShopItemData { public string id; public string name; public int cost; }
    [Serializable] public class HouseData { public string name; public int gx; public int gz; public int price; public string car; }
    [Serializable]
    public class WorldFile
    {
        public float block; public float street; public float pitch; public int grid; public float zoneRadius; public float lane;
        public float daySeconds; public float signalCycle; public float captureTime; public float raidGather;
        public int recruitCost; public int upgradeCost; public float upgradeHp; public int buildCost; public float buildBoost;
        public float armorMax; public float driveByRange; public BackupData backup;
        public List<string> districts = new(); public List<string> hustles = new();
        public List<ShopItemData> shopItems = new(); public List<GridSpot> shops = new(); public List<HouseData> houses = new();
    }

    /// <summary>Everything the simulation reads, loaded once from Assets/Data.</summary>
    public class GameData
    {
        public CrewFile Crews; public WeaponFile Weapons; public VehicleFile Vehicles;
        public JobFile Jobs; public PoliceFile Police; public WorldFile World;

        public CrewData Crew(string id) => Crews.crews.Find(c => c.id == id);
        public WeaponData Weapon(string id) => Weapons.weapons.Find(w => w.id == id);
        public VehicleData Vehicle(string id) => Vehicles.vehicles.Find(v => v.id == id);
    }
}
