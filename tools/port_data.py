#!/usr/bin/env python3
"""Convert the Street Empire JS data (dumped from src/state.js) into Unity friendly JSON.

Usage:
  node tools/dump_state.mjs /path/to/StreetEmpire/src/state.js > /tmp/se-exports.json
  python3 tools/port_data.py /tmp/se-exports.json Assets/Data

JsonUtility cannot read dictionaries, so every JS object keyed by id becomes an array with an "id" field.
Positions stay in the source (three.js) space. Convert with StreetEmpire.Sim.WorldSpace in C#.
"""
import json, sys, os

src = json.load(open(sys.argv[1]))
out = sys.argv[2]
SOURCE = "matpcul-tech/StreetEmpire src/state.js"

def hexc(n):
    return "#%06x" % n

def write(name, obj):
    obj = {"source": SOURCE, **obj}
    with open(os.path.join(out, name), "w") as f:
        json.dump(obj, f, indent=2)
        f.write("\n")

def pts(lst):
    return [{"x": p["x"], "z": p["z"]} for p in lst]

# crews
write("crews.json", {"crews": [
    {"id": k, "name": v["name"], "color": hexc(v["color"]), "uiColor": v["css"],
     "shirt": hexc(v["shirt"]), "pants": hexc(v["pants"]), "legacyModel": v["model"]}
    for k, v in src["CREWS"].items()]})

# weapons
write("weapons.json", {"aimAssist": src["AIM_ASSIST"], "weapons": [
    {"id": k, "range": v["range"], "damage": v["dmg"], "cooldown": v["cd"], "swing": v["swing"],
     "knockback": v["kb"], "ranged": v["range"] > 5}
    for k, v in src["WEAPONS"].items()]})

# vehicles
veh = []
for k, v in src["CAR_TYPES"].items():
    veh.append({"id": k, "kind": "bike" if k == "bike" else "car", "maxSpeed": v["max"], "accel": v["acc"],
                "hp": v["hp"], "wheelbase": v.get("wb", 2.7)})
for k, v in src["BIKE_MODELS"].items():
    veh.append({"id": k, "kind": "bikeModel", "maxSpeed": v["max"], "accel": v["acc"], "hp": v["hp"],
                "wheelbase": v.get("wb", 2.7)})
write("vehicles.json", {"vehicles": veh, "garageCost": src["GARAGE_COST"],
    "garages": [{"name": g["name"], "x": g["x"], "z": g["z"]} for g in src["GARAGES"]],
    "burnTime": src["BURN_TIME"], "blastRadius": src["BLAST_R"], "wreckClear": src["WRECK_CLEAR"],
    "ramps": [{"x": r["x"], "z": r["z"], "ry": r["ry"]} for r in src["RAMPS"]]})

# jobs: contacts, story missions, side jobs, street calls, races
missions = []
for i, m in enumerate(src["MISSIONS"]):
    missions.append({"index": i, "title": m["title"], "by": m["by"], "kind": m["kind"],
        "time": m.get("time", 0), "reward": m["reward"], "rep": m["rep"], "brief": m.get("brief", []),
        "intro": m.get("intro", ""), "gift": m.get("gift", ""), "need": m.get("need", 0),
        "district": m.get("district", -1), "car": m.get("car", ""), "handoff": m.get("handoff", ""),
        "minHp": m.get("minHp", 0), "crew": m.get("crew", ""), "heat": m.get("heat", 0),
        "who": m.get("who", ""), "home": m.get("home", False), "rival": m.get("rival", False)})
contacts = [{"name": c["name"], "gx": c["gx"], "gz": c["gz"], "slot": c.get("slot", ""),
             "female": c.get("female", False), "look": c.get("look", 0),
             "pass": c.get("pass", []), "fail": c.get("fail", [])} for c in src["CONTACTS"]]
races = [{"name": r["name"], "start": {"x": r["start"]["x"], "z": r["start"]["z"]}, "points": pts(r["points"]),
          "length": r["len"], "gold": r["gold"], "silver": r["silver"], "bronze": r["bronze"]} for r in src["RACES"]]
rp = src["RACE_PAY"]
c = src["CALLS"]
write("jobs.json", {"contacts": contacts, "missions": missions,
    "fare": {"wait": src["FARE"]["wait"], "gap": src["FARE"]["gap"], "basePay": src["FARE"]["base"],
             "perMeter": src["FARE"]["perM"], "tipMax": src["FARE"]["tipMax"]},
    "courier": {"wait": src["COURIER"]["wait"], "gap": src["COURIER"]["gap"], "basePay": src["COURIER"]["base"],
                "perMeter": src["COURIER"]["perM"], "bonusMax": src["COURIER"]["bonusMax"], "builtMultiplier": src["COURIER"]["built"]},
    "races": races, "racePay": {"gold": rp["gold"], "silver": rp["silver"], "bronze": rp["bronze"], "none": rp["none"]},
    "streetCalls": {"first": c["first"], "cooldown": c["cd"], "offer": c["offer"], "fight": c["fight"], "cap": c["cap"],
                    "maxNpc": c["maxNpc"], "minDist": c["minDist"], "strikeCap": c["strikeCap"], "kinds": c["kinds"]},
    "stashPay": src["STASH_PAY"], "stashAll": src["STASH_ALL"], "stashes": pts(src["STASHES"]),
    "dice": {"bets": src["DICE"]["bets"], "reach": src["DICE"]["reach"], "leave": src["DICE"]["leave"]}})

# police: derived from stars(), policeStep(), heliStep(), onPlayerDown() and the addHeat() call sites
levels = []
for st in range(1, 6):
    tac = st >= 4
    levels.append({"stars": st, "minHeat": 10 + (st - 1) * 20, "maxUnits": min(12, st * 2 + (2 if tac else 0)),
                   "spawnInterval": 3.5 if tac else 5, "officersPerCar": 3 if tac else 2, "officerHp": 120 if tac else 80,
                   "cruiserHp": 220 if tac else src["CAR_TYPES"]["police"]["hp"], "officerWeapon": "pistol" if st >= 3 else "fists",
                   "tactical": tac, "helicopter": st >= 4, "roadblocks": st >= 5})
write("police.json", {"maxHeat": 100, "levels": levels,
    "helicopter": {"speed": src["HELI"]["speed"], "sight": src["HELI"]["see"], "fuel": src["HELI"]["fuel"],
                   "cooldownAfterFuelOut": src["HELI"]["away"], "cooldownAfterLeave": 8, "leavesBelowStars": 3, "orbitRadius": 14},
    "decay": {"copNearRadius": 30, "calmAfter": 8, "calmRate": 5, "longCalmAfter": 20, "longCalmRate": 3, "safehouseRate": 14},
    "spawnRingMin": 60, "spawnRingMax": 110,
    "bustedHeat": 10, "bailBusted": 0.4, "bailWasted": 0.25, "downTime": src["DOWN_TIME"], "clearTime": src["CLEAR_TIME"],
    "heatSources": [
        {"id": "jackCar", "heat": 6}, {"id": "jackPoliceCar", "heat": 25}, {"id": "driveByShot", "heat": 8},
        {"id": "gunshot", "heat": 8}, {"id": "shotgunShot", "heat": 10},
        {"id": "hitPed", "heat": 7}, {"id": "hitCop", "heat": 18}, {"id": "hitOtherGun", "heat": 3}, {"id": "hitOtherMelee", "heat": 2},
        {"id": "koPed", "heat": 8}, {"id": "koCop", "heat": 15}, {"id": "koOtherGun", "heat": 5}, {"id": "koOtherMelee", "heat": 4},
        {"id": "runOverPed", "heat": 10}, {"id": "runOverOther", "heat": 3}, {"id": "carExplodes", "heat": 14},
        {"id": "witnessCall", "heat": 12}, {"id": "streetCallDeal", "heat": 6}],
    "notes": "Heat source call sites are in state.js addHeat(). Verify any change against the JS before tuning."})

# world and economy
write("world.json", {"block": src["BLOCK"], "street": src["STREET"], "pitch": src["PITCH"], "grid": src["GRID"],
    "zoneRadius": src["ZONE_R"], "lane": src["LANE"], "daySeconds": src["DAY_SECONDS"], "signalCycle": src["SIGNAL_CYCLE"],
    "captureTime": src["CAPTURE_TIME"], "raidGather": src["RAID_GATHER"], "recruitCost": src["RECRUIT_COST"],
    "upgradeCost": src["UPGRADE_COST"], "upgradeHp": src["UPGRADE_HP"], "buildCost": src["BUILD_COST"], "buildBoost": src["BUILD_BOOST"],
    "armorMax": src["ARMOR_MAX"], "driveByRange": src["DRIVEBY_RANGE"],
    "backup": {"range": src["BACKUP"]["range"], "run": src["BACKUP"]["run"], "near": src["BACKUP"]["near"]},
    "districts": src["DISTRICTS"], "hustles": src["HUSTLES"],
    "shopItems": [{"id": k, "name": v["name"], "cost": v["cost"]} for k, v in src["SHOP_ITEMS"].items()],
    "shops": [{"name": s["name"], "gx": s["gx"], "gz": s["gz"]} for s in src["SHOPS"]],
    "houses": [{"name": h["name"], "gx": h["gx"], "gz": h["gz"], "price": h["price"], "car": h.get("car", "")} for h in src["HOUSES"]]})
print("ok")
