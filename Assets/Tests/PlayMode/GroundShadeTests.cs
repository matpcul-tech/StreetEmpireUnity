// Ground shades: cars and people near the camera sit on a soft dark patch, so they read as standing on the street.
using System.Collections;
using NUnit.Framework;
using StreetEmpire.City;
using StreetEmpire.Game;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace StreetEmpire.Tests
{
    [PrebuildSetup(typeof(SceneSetup))]
    public class GroundShadeTests
    {
        [UnityTest, Timeout(120000)]
        public IEnumerator CarsAndPeopleSitOnAShade()
        {
            MuggerOpening.AutoStart = false; SceneManager.LoadScene("Eastside");
            yield return null; yield return null;
            var shades = GroundShades.Instance; Assert.IsNotNull(shades, "the ground shades");
            yield return new WaitForSeconds(1.5f);
            Debug.Log($"[GroundShadeTests] shades out: {shades.Drawn}, under cars: {shades.UnderCars}");
            Assert.GreaterOrEqual(shades.Drawn, 2, "patches are out"); Assert.GreaterOrEqual(shades.UnderCars, 1, "a parked car near the start has one");
            var player = GameObject.FindGameObjectWithTag("Player"); Assert.IsNotNull(player, "the player");
            bool under = false;
            foreach (Transform s in shades.transform)
            {
                if (!s.gameObject.activeSelf) continue;
                var d = s.position - player.transform.position;
                if (new Vector2(d.x, d.z).magnitude < 0.2f && Mathf.Abs(d.y) < 0.3f) under = true;
                Assert.IsNotNull(s.GetComponent<MeshRenderer>().sharedMaterial, "every patch has its material");
            }
            Assert.IsTrue(under, "the player stands on a patch, at his feet");
        }
    }
}
