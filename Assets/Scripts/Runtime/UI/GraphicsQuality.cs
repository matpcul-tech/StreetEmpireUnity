// The graphics setting: Ultra (default) or High, kept between runs. Each tier is its own URP asset made by
// GraphicsSetup (Ultra: 4 cascade soft shadows over 120 m, 4x MSAA, SSAO; High: 2 cascades over 80 m,
// 2x MSAA, no SSAO). The HUD button calls Toggle.
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
            PlayerPrefs.SetInt(Key, Tier);
            if (label) label.text = Tier == Ultra ? "GFX ULTRA" : "GFX HIGH";
        }
    }
}
