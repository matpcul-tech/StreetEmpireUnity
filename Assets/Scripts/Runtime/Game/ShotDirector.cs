// Screenshot set-ups for the headless WebGL capture (tools/capture_webgl.mjs --query=shot=...).
// Only acts when the page URL asks for it, so normal play is untouched:
//   shot=job     next to Unc with Bag Back started (brief, marker, timer)
//   shot=bag     Bag Back running, standing by the bag
//   shot=police  two stars, officers on the way
//   shot=car     in the sedan
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
                case "car":
                    var car = FindFirstObjectByType<CarController>();
                    p.Teleport(car.transform.position - car.transform.right * 2.4f, car.transform.eulerAngles.y);
                    yield return null;
                    p.GetComponent<VehicleUser>().TryEnter();
                    break;
            }
            Debug.Log($"[ShotDirector] {shot}");
        }
    }
}
