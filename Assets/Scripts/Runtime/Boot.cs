using StreetEmpire.Sim;
using UnityEngine;

namespace StreetEmpire
{
    /// <summary>
    /// Scaffold boot: loads the ported data, builds a LocalSession and draws the 9 x 9 turf grid as
    /// colored blocks so CI builds show the data and the sim seam are wired. Not gameplay.
    /// </summary>
    public class Boot : MonoBehaviour
    {
        public GameDatabase database;
        public long seed = 1;
        LocalSession session;
        string summary;

        void Start()
        {
            var data = database.Load();
            session = new LocalSession(data, seed);
            var s = session.State; var w = data.World;
            foreach (var b in s.blocks)
            {
                var (sx, sz) = Simulation.BlockCenter(w, b);
                var (x, z) = WorldSpace.FromSource(sx, sz);
                var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = $"Block {b.id} {b.hustle}";
                go.transform.SetParent(transform, false);
                go.transform.localPosition = new Vector3(x, 1f, z);
                go.transform.localScale = new Vector3(w.block, 2f, w.block);
                go.GetComponent<Renderer>().material.color = GameDataLoader.ParseColor(data.Crew(b.owner).color);
            }
            summary = $"Street Empire Unity scaffold\n{data.Crews.crews.Count} crews, {data.Jobs.missions.Count} jobs, " +
                      $"{data.Weapons.weapons.Count} weapons, {data.Vehicles.vehicles.Count} vehicles, {data.Police.levels.Count} police levels";
        }

        void Update() => session?.Tick(Time.deltaTime);

        void OnGUI() => GUI.Label(new Rect(12, 12, 900, 60), summary);
    }
}
