// Logs memory every 2 s for the first 30 s and writes the OK marker once Eastside has run 12 s.
using UnityEngine;
using UnityEngine.SceneManagement;

namespace StreetEmpire.Boot
{
    public class BootHeartbeat : MonoBehaviour
    {
        float next = 2f;
        bool ok;
        void Update()
        {
            float t = BootDiagnostics.Since;
            if (t >= next && next <= 30f) { BootDiagnostics.Write($"alive {BootDiagnostics.Mem()} fps {1f / Mathf.Max(0.0001f, Time.unscaledDeltaTime):0}"); next += 2f; }
            if (!ok && t > 12f && SceneManager.GetActiveScene().name == "Eastside") { ok = true; BootDiagnostics.Write(BootDiagnostics.OkMarker); }
        }
    }
}
