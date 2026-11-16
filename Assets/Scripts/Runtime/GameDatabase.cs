using StreetEmpire.Sim.Data;
using UnityEngine;

namespace StreetEmpire
{
    /// <summary>Holds the six JSON files from Assets/Data and turns them into GameData.</summary>
    [CreateAssetMenu(menuName = "Street Empire/Game Database", fileName = "GameDatabase")]
    public class GameDatabase : ScriptableObject
    {
        public TextAsset crews, weapons, vehicles, jobs, police, world;

        public GameData Load() => GameDataLoader.FromJson(crews.text, weapons.text, vehicles.text, jobs.text, police.text, world.text);
    }

    public static class GameDataLoader
    {
        public static GameData FromJson(string crews, string weapons, string vehicles, string jobs, string police, string world) => new GameData
        {
            Crews = JsonUtility.FromJson<CrewFile>(crews),
            Weapons = JsonUtility.FromJson<WeaponFile>(weapons),
            Vehicles = JsonUtility.FromJson<VehicleFile>(vehicles),
            Jobs = JsonUtility.FromJson<JobFile>(jobs),
            Police = JsonUtility.FromJson<PoliceFile>(police),
            World = JsonUtility.FromJson<WorldFile>(world),
        };

        public static Color ParseColor(string hex) => ColorUtility.TryParseHtmlString(hex, out var c) ? c : Color.magenta;
    }
}
