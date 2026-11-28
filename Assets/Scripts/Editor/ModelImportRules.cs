// Import rules for character FBX files: Humanoid rigs on the Mixamo skeleton (65 bones), which the characters and
// the clips share (the CC0 Quaternius UAL clips are baked onto it by tools/bake_to_mixamo.py), in-place looping
// clips, and normal maps marked as such.
// Textures under Assets/Art are GPU compressed per platform (ASTC Android, DXT WebGL).
using System.Linq;
using UnityEditor;

namespace StreetEmpire.EditorTools
{
    public class ModelImportRules : AssetPostprocessor
    {
        // bump when the rules change: the CI Library cache otherwise keeps textures imported under the old rules
        // (2: cutout coverage, 4x4 ASTC alpha maps, anisotropic building walls)
        public override uint GetVersion() => 3;   // 3: v75g/h 2048 wall normal and roughness maps for Matthew's files
        static bool IsCharacter(string p) => p.StartsWith("Assets/Art/Models/Characters/") && p.EndsWith(".fbx");
        static bool IsClips(string p) => p.StartsWith("Assets/Art/Animations/") && p.EndsWith(".fbx");

        void OnPreprocessModel()
        {
            if (!(assetImporter is ModelImporter m) || !(IsCharacter(assetPath) || IsClips(assetPath))) return;
            m.animationType = ModelImporterAnimationType.Human;
            m.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            // the baked clips make their own avatar from their own rest pose. Read against the player's avatar instead,
            // Unity found the bones 14 to 36 cm out of place (Blender writes the bones' own axes its way on export) and
            // every body hung four metres in the air (Nov 28).
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
            // data maps hold numbers, not colors: URP metallic (R) and smoothness (A), or a roughness or occlusion map
            bool data = assetPath.Contains("_metalsmooth") || assetPath.Contains("_roughness") || assetPath.Contains("_occlusion");
            // v73 rule: normal and roughness (metalsmooth) maps import with sRGB off, every color map
            // (base color, emission) with sRGB on. A normal map type is always sampled linear.
            if (normal) { t.textureType = TextureImporterType.NormalMap; t.sRGBTexture = false; }
            else if (data) { t.textureType = TextureImporterType.Default; t.sRGBTexture = false; t.alphaSource = TextureImporterAlphaSource.FromInput; }
            else t.sRGBTexture = true;
            // hero props seen up close keep 2048 px color (tools/extract_prop_textures.mjs names them _2k); the
            // other prop maps are sized by how close they get seen (Size), which keeps the APK down
            bool prop = assetPath.StartsWith("Assets/Art/Textures/Props/");
            bool hero = assetPath.Contains("_2k_");
            // v74 buildings: the clean wall color at 2048 (spec A.5 caps every building map at 2048), its normal
            // and the roughness maps at 1024, the Meshy trim atlas (Material_0) at its own 1024
            // v75g: the white house walls keep their normal and roughness at 2048 too (its stucco lives in the normal map); v75h: Matthew's v3 house-02, apt-mixed-use and barber too
            bool building = assetPath.StartsWith("Assets/Art/Textures/Buildings/");
            int max = prop ? Size(assetPath, normal, data, hero) : building ? (assetPath.Contains("__walls_basecolor") || (assetPath.Contains("__walls_") && (assetPath.Contains("house-01-bungalow") || assetPath.Contains("house-02-two-story") || assetPath.Contains("apt-mixed-use") || assetPath.Contains("store-barber"))) ? 2048 : 1024) : hero ? 2048 : 1024;
            t.mipmapEnabled = true; t.maxTextureSize = max;
            // ground textures tile across big boxes and are seen at grazing angles
            if (assetPath.StartsWith("Assets/Art/Textures/Ground/")) { t.wrapMode = UnityEngine.TextureWrapMode.Repeat; t.anisoLevel = 8; }
            // v75u car lettering: a cut-out panel over the doors. Clamped so the words do not repeat past the panel's
            // edge, and its alpha kept in the mips so the letters do not thin out with distance.
            if (assetPath.StartsWith("Assets/Art/Textures/Cars/V2/") && assetPath.EndsWith("_decal.png"))
            { t.wrapMode = UnityEngine.TextureWrapMode.Clamp; t.alphaIsTransparency = true; t.mipMapsPreserveCoverage = true; t.alphaTestReferenceValue = 0.5f; t.filterMode = UnityEngine.FilterMode.Trilinear; t.anisoLevel = 4; }
            if (assetPath.EndsWith("yard_variation.png")) t.sRGBTexture = false;   // URP detail albedo: linear, 0.5 is neutral
            if (assetPath.StartsWith("Assets/Art/Textures/Props/") && assetPath.Contains("chainlink")) t.wrapMode = UnityEngine.TextureWrapMode.Repeat;
            // building walls are seen at grazing angles down the street: 8x anisotropic so the mips do not shimmer
            // v75 far towers: their window grid and lit window map tile and read in the fog
            if (assetPath.Contains("Props/edge-")) { t.wrapMode = UnityEngine.TextureWrapMode.Repeat; t.anisoLevel = 4; }
            if (building) { t.anisoLevel = 8; t.filterMode = UnityEngine.FilterMode.Trilinear; }   // spec E.2: mipmaps on, 8x anisotropic
            // palm fronds are alpha tested: keep their coverage in the mips so the crowns do not thin out with
            // distance, and keep the alpha channel on WebGL (DXT5, not DXT1)
            // every alpha tested map (palm fronds, dry plants, the chain link wire, decals): keep the coverage in the
            // mips so they do not thin or stripe with distance, trilinear with 4x anisotropic, and the alpha kept on
            // WebGL (DXT5, not DXT1) and on Android in 4x4 ASTC blocks (8x8 blocks smear the cut edge into stripes)
            bool frond = assetPath.StartsWith("Assets/Art/Textures/Props/") && !normal && !data && (assetPath.Contains("palm_frond") || assetPath.Contains("dry_blades") || assetPath.Contains("decal-set") || assetPath.Contains("fence_wire") || assetPath.Contains("fx_dust"));
            // v75u: the car lettering panels are cut out too (4x4 ASTC on Android, DXT5 on WebGL so the alpha survives)
            frond |= assetPath.StartsWith("Assets/Art/Textures/Cars/V2/") && assetPath.EndsWith("_decal.png");
            if (frond) { t.alphaIsTransparency = true; t.mipMapsPreserveCoverage = true; t.alphaTestReferenceValue = 0.5f; t.filterMode = UnityEngine.FilterMode.Trilinear; t.anisoLevel = 4; }
            t.textureCompression = TextureImporterCompression.Compressed;
            // M5: compressed on the GPU, not RGBA32. Android ASTC (6x6 color, 5x5 normals), WebGL DXT
            // (DXT1 color, DXT5 normals), which desktop browsers decode in hardware.
            t.SetPlatformTextureSettings(new TextureImporterPlatformSettings
            {
                name = "Android", overridden = true, maxTextureSize = max,
                // props: 8x8 blocks (2 bits a pixel), except hero maps, cutout wire and the lettered signs (6x6)
                format = frond ? TextureImporterFormat.ASTC_4x4
                    : !prop ? (normal ? TextureImporterFormat.ASTC_5x5 : TextureImporterFormat.ASTC_6x6)
                    : (hero && !data) || Crisp(assetPath) ? TextureImporterFormat.ASTC_6x6 : TextureImporterFormat.ASTC_8x8,
                compressionQuality = 50,
            });
            t.SetPlatformTextureSettings(new TextureImporterPlatformSettings
            {
                name = "WebGL", overridden = true, maxTextureSize = max,
                format = normal || data || frond ? TextureImporterFormat.DXT5 : TextureImporterFormat.DXT1,
                compressionQuality = 50,
            });
        }

        static readonly string[] SmallProps = { "tyre", "trashbag", "cardboard-box", "crate", "barrel", "manhole", "security-light",
            "newspaper-box", "mailbox", "utility-box-1", "utility-box-2", "aircon", "bus-stop-sign", "sign-post" };

        static bool Crisp(string path) => path.Contains("chainlink-panel__modular_chainlink_fence_wire") || path.Contains("decal-set") || path.Contains("street-blade") || path.Contains("stop-sign__stop") || path.Contains("pawn-sign") || path.Contains("liquor-sign") || path.Contains("laundromat-sign") || path.Contains("takeout-sign") || path.Contains("check-cashing-sign") || path.Contains("barber-sign") || path.Contains("auto-sign") || path.Contains("gas-store-sign") || path.Contains("gas-price-sign");

        /// <summary>Prop texture sizes: hero color 2048 with 1024 normals, glass 256, metal and smoothness 512,
        /// small clutter 512, the rest 1024 color and 512 normals.</summary>
        static int Size(string path, bool normal, bool data, bool hero)
        {
            var file = System.IO.Path.GetFileName(path);
            if (file.Contains("glass")) return 256;
            if (data) return 512;
            if (hero) return normal ? 1024 : 2048;
            if (normal) return 512;
            foreach (var p in SmallProps) if (file.StartsWith(p + "__")) return 512;
            return 1024;
        }

        /// <summary>Blender takes come out as "Armature|Idle_Loop_Armature"; keep "Idle_Loop".</summary>
        public static string ClipName(string take)
        {
            var n = take.Contains("|") ? take.Substring(take.LastIndexOf('|') + 1) : take;
            return n.EndsWith("_Armature") ? n.Substring(0, n.Length - "_Armature".Length) : n;
        }
    }
}
