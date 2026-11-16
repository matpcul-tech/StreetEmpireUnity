// Import rules for character FBX files: Humanoid rigs so the CC0 Quaternius clips (UAL) retarget onto
// the Meshy player (Mixamo bone names), in-place looping clips, and normal maps marked as such.
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
            if (assetPath.StartsWith("Assets/Art/Models/Characters/") && assetPath.Contains("_normal") && assetImporter is TextureImporter t)
                t.textureType = TextureImporterType.NormalMap;
        }

        /// <summary>Blender takes come out as "Armature|Idle_Loop_Armature"; keep "Idle_Loop".</summary>
        public static string ClipName(string take)
        {
            var n = take.Contains("|") ? take.Substring(take.LastIndexOf('|') + 1) : take;
            return n.EndsWith("_Armature") ? n.Substring(0, n.Length - "_Armature".Length) : n;
        }
    }
}
