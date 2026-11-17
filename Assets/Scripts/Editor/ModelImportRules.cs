// Import rules for character FBX files: Humanoid rigs so the CC0 Quaternius clips (UAL) retarget onto
// the Meshy player (Mixamo bone names), in-place looping clips, and normal maps marked as such.
// Textures under Assets/Art are GPU compressed per platform (ASTC Android, DXT WebGL).
using System.Linq;
using UnityEditor;

namespace StreetEmpire.EditorTools
{
    public class ModelImportRules : AssetPostprocessor
    {
        static bool IsCharacter(string p) => p.StartsWith("Assets/Art/Models/Characters/") && p.EndsWith(".fbx");
        static bool IsClips(string p) => p.StartsWith("Assets/Art/Animations/") && p.EndsWith(".fbx");

        void OnPreprocessModel()
        {
            if (!(assetImporter is ModelImporter m) || !(IsCharacter(assetPath) || IsClips(assetPath))) return;
            m.animationType = ModelImporterAnimationType.Human;
            m.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            m.materialImportMode = ModelImporterMaterialImportMode.None; // materials are made by PlayerBuilder
            m.importAnimation = IsClips(assetPath);
            m.importBlendShapes = false;
            m.importCameras = false; m.importLights = false;
            m.useFileScale = true; m.globalScale = 1f;
        }

        void OnPreprocessAnimation()
        {
            if (!(assetImporter is ModelImporter m) || !IsClips(assetPath)) return;
            var clips = m.defaultClipAnimations.Select(c =>
            {
                c.name = ClipName(c.takeName);
                c.loopTime = c.name.Contains("Loop");
                c.lockRootRotation = true; c.lockRootHeightY = true; c.lockRootPositionXZ = true; // in place: the controller moves the body
                c.keepOriginalOrientation = true; c.keepOriginalPositionY = true; c.keepOriginalPositionXZ = true;
                return c;
            }).ToArray();
            m.clipAnimations = clips;
        }

        void OnPreprocessTexture()
        {
            if (!(assetImporter is TextureImporter t)) return;
            bool art = assetPath.StartsWith("Assets/Art/Models/") || assetPath.StartsWith("Assets/Art/Textures/");
            if (!art) return;
            bool normal = assetPath.Contains("_normal");
            bool data = assetPath.Contains("_metalsmooth");   // URP metallic (R) and smoothness (A): linear, keeps alpha
            if (normal) t.textureType = TextureImporterType.NormalMap;
            if (data) { t.sRGBTexture = false; t.alphaSource = TextureImporterAlphaSource.FromInput; }
            t.mipmapEnabled = true; t.maxTextureSize = 1024;
            t.textureCompression = TextureImporterCompression.Compressed;
            // M5: compressed on the GPU, not RGBA32. Android ASTC (6x6 color, 5x5 normals), WebGL DXT
            // (DXT1 color, DXT5 normals), which desktop browsers decode in hardware.
            t.SetPlatformTextureSettings(new TextureImporterPlatformSettings
            {
                name = "Android", overridden = true, maxTextureSize = 1024,
                format = normal ? TextureImporterFormat.ASTC_5x5 : TextureImporterFormat.ASTC_6x6,
                compressionQuality = 50,
            });
            t.SetPlatformTextureSettings(new TextureImporterPlatformSettings
            {
                name = "WebGL", overridden = true, maxTextureSize = 1024,
                format = normal || data ? TextureImporterFormat.DXT5 : TextureImporterFormat.DXT1,
                compressionQuality = 50,
            });
        }

        /// <summary>Blender takes come out as "Armature|Idle_Loop_Armature"; keep "Idle_Loop".</summary>
        public static string ClipName(string take)
        {
            var n = take.Contains("|") ? take.Substring(take.LastIndexOf('|') + 1) : take;
            return n.EndsWith("_Armature") ? n.Substring(0, n.Length - "_Armature".Length) : n;
        }
    }
}
