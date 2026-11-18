using NUnit.Framework;
using UnityEditor;

namespace StreetEmpire.Tests
{
    public class TextureImportTests
    {
        static TextureImporter Imp(string path)
        {
            var t = AssetImporter.GetAtPath(path) as TextureImporter;
            Assert.IsNotNull(t, path);
            return t;
        }

        /// <summary>v73 import rule: normal and roughness (metalsmooth) maps are linear, color maps are sRGB.</summary>
        [Test]
        public void NormalAndRoughnessMapsAreLinearAndColorMapsAreSrgb()
        {
            var nm = Imp("Assets/Art/Textures/Buildings/apt-02-brick__walls_normal.jpg");
            Assert.AreEqual(TextureImporterType.NormalMap, nm.textureType);
            Assert.IsFalse(nm.sRGBTexture, "normal map sRGB");
            Assert.IsFalse(Imp("Assets/Art/Textures/Buildings/apt-02-brick__walls_metalsmooth.png").sRGBTexture, "metalsmooth sRGB");
            Assert.IsFalse(Imp("Assets/Art/Textures/Props/wood-fence__fence_boards_metalsmooth.png").sRGBTexture, "prop metalsmooth sRGB");
            Assert.IsFalse(Imp("Assets/Art/Textures/Props/wood-fence__fence_boards_normal.jpg").sRGBTexture, "prop normal sRGB");
            Assert.IsTrue(Imp("Assets/Art/Textures/Buildings/apt-02-brick__walls_basecolor.jpg").sRGBTexture, "base color sRGB");
            Assert.IsTrue(Imp("Assets/Art/Textures/Buildings/apt-02-brick__Material_0_basecolor.jpg").sRGBTexture, "trim color sRGB");
            var walls = Imp("Assets/Art/Textures/Buildings/apt-02-brick__walls_basecolor.jpg");
            Assert.IsTrue(walls.mipmapEnabled, "building mipmaps"); Assert.GreaterOrEqual(walls.anisoLevel, 4, "building anisotropic filtering");
            Assert.LessOrEqual(walls.maxTextureSize, 2048, "building max size");
            Assert.IsTrue(Imp("Assets/Art/Models/Characters/Player/player_basecolor.jpg").sRGBTexture, "character color sRGB");
            Assert.IsFalse(Imp("Assets/Art/Models/Characters/Player/player_normal.jpg").sRGBTexture, "character normal sRGB");
        }
    }
}
