using System.IO;
using StreetEmpire.Definitions;
using UnityEditor;
using UnityEngine;

namespace StreetEmpire.EditorTools
{
    /// <summary>Street Empire > Build Data Assets: one ScriptableObject per crew, weapon, vehicle, job and contact.</summary>
    public static class DataAssetBuilder
    {
        const string Root = "Assets/Data/Definitions";

        [MenuItem("Street Empire/Build Data Assets")]
        public static void Build()
        {
            ProjectBootstrap.Ensure();
            var data = AssetDatabase.LoadAssetAtPath<GameDatabase>("Assets/Data/GameDatabase.asset").Load();
            foreach (var c in data.Crews.crews) Save<CrewDefinition>("Crews", c.id, d => d.data = c);
            foreach (var w in data.Weapons.weapons) Save<WeaponDefinition>("Weapons", w.id, d => d.data = w);
            foreach (var v in data.Vehicles.vehicles) Save<VehicleDefinition>("Vehicles", v.id, d => d.data = v);
            foreach (var m in data.Jobs.missions) Save<JobDefinition>("Jobs", $"{m.index:00}_{m.title.Replace(' ', '_')}", d => d.data = m);
            foreach (var c in data.Jobs.contacts) Save<ContactDefinition>("Contacts", c.name, d => d.data = c);
            Save<PoliceConfig>("Police", "PoliceConfig", d => d.data = data.Police);
            AssetDatabase.SaveAssets();
            Debug.Log("[DataAssetBuilder] Definitions written under " + Root);
        }

        static void Save<T>(string folder, string name, System.Action<T> fill) where T : ScriptableObject
        {
            var dir = $"{Root}/{folder}"; Directory.CreateDirectory(dir);
            var path = $"{dir}/{name}.asset";
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            bool created = asset == null;
            if (created) asset = ScriptableObject.CreateInstance<T>();
            fill(asset);
            if (created) AssetDatabase.CreateAsset(asset, path); else EditorUtility.SetDirty(asset);
        }
    }
}
