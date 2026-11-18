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
            var nm = Imp("Assets/Art/Textures/Buildings/house-01-bungalow_normal.jpg");
            Assert.AreEqual(TextureImporterType.NormalMap, nm.textureType);
            Assert.IsFalse(nm.sRGBTexture, "normal map sRGB");
            Assert.IsFalse(Imp("Assets/Art/Textures/Buildings/house-01-bungalow_metalsmooth.png").sRGBTexture, "metalsmooth sRGB");
            Assert.IsFalse(Imp("Assets/Art/Textures/Props/wood-fence__fence_boards_metalsmooth.png").sRGBTexture, "prop metalsmooth sRGB");
            Assert.IsFalse(Imp("Assets/Art/Textures/Props/wood-fence__fence_boards_normal.jpg").sRGBTexture, "prop normal sRGB");
            Assert.IsTrue(Imp("Assets/Art/Textures/Buildings/house-01-bungalow_basecolor.jpg").sRGBTexture, "base color sRGB");
            Assert.IsTrue(Imp("Assets/Art/Textures/Buildings/house-01-bungalow_emission.jpg").sRGBTexture, "emission sRGB");
            Assert.IsTrue(Imp("Assets/Art/Models/Characters/Player/player_basecolor.jpg").sRGBTexture, "character color sRGB");
            Assert.IsFalse(Imp("Assets/Art/Models/Characters/Player/player_normal.jpg").sRGBTexture, "character normal sRGB");
        }
    }
}
