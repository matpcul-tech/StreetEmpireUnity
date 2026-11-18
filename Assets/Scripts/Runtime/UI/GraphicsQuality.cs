// The graphics setting: Ultra (default) or High, kept between runs. Each tier is its own URP asset made by
// GraphicsSetup (see there). On top, Ultra keeps props and building detail 30% farther (LOD bias 1.3, prop
// draw distance x1.3), High drops them 20% sooner (0.8). The HUD button calls Toggle.
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace StreetEmpire.UI
{
    public class GraphicsQuality : MonoBehaviour
    {
        public RenderPipelineAsset high, ultra;
        public Text label;
        public const string Key = "graphics.tier";
        public const int High = 0, Ultra = 1;
        public static int Tier { get; private set; } = Ultra;

        void Awake() => Apply(PlayerPrefs.GetInt(Key, Ultra));

        public void Toggle() => Apply(Tier == Ultra ? High : Ultra);

        public void Apply(int tier)
        {
            Tier = tier == High ? High : Ultra;
            var asset = Tier == Ultra ? ultra : high;
            if (asset != null && QualitySettings.renderPipeline != asset) QualitySettings.renderPipeline = asset;
            float reach = Tier == Ultra ? 1.3f : 0.8f;
            QualitySettings.lodBias = reach;
            foreach (var inst in FindObjectsByType<StreetEmpire.City.PropInstancer>(FindObjectsSortMode.None)) inst.distanceScale = reach;
            PlayerPrefs.SetInt(Key, Tier);
            if (label) label.text = Tier == Ultra ? "GFX ULTRA" : "GFX HIGH";
        }
    }
}
