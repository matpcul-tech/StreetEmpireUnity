// M3: walk up to the parked sedan, get in, drive it with the sedan numbers, steer, exit to the left.
using System.Collections;
using NUnit.Framework;
using StreetEmpire.Player;
using StreetEmpire.Vehicles;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace StreetEmpire.Tests
{
    [PrebuildSetup(typeof(SceneSetup))]
    public class CarTests
    {
        [UnityTest]
        public IEnumerator EnterDriveSteerAndExitSedan()
        {
            SceneManager.LoadScene("Eastside");
            yield return null; yield return null;
            var car = Object.FindFirstObjectByType<CarController>();
            var player = Object.FindFirstObjectByType<PlayerMotor>();
            var user = player.GetComponent<VehicleUser>();
            Assert.IsNotNull(car, "sedan in scene"); Assert.IsNotNull(user, "VehicleUser on player");
            Assert.AreEqual(28f, car.stats.maxSpeed, 0.01f, "sedan maxSpeed from vehicles.json");
            for (int i = 0; i < 20; i++) yield return null;

            // too far: no entry
            player.Teleport(car.transform.position + car.transform.right * 9f, 0f);
            yield return null;
            Assert.IsFalse(user.TryEnter(), "no entry from 9 m");

            player.Teleport(car.transform.position + car.transform.right * 3f, 0f);
            yield return null;
            Assert.IsTrue(user.TryEnter(), "enter from 3 m");
            Assert.AreSame(car, user.Car); Assert.IsTrue(car.hasDriver);
            Assert.IsFalse(player.GetComponent<CharacterController>().enabled, "on-foot collider off while driving");

            // the parked sedan faces +Z down the street; drive straight
            var start = car.transform.position;
            car.scriptThrottle = 1f; car.scriptSteer = 0f;
            yield return new WaitForSeconds(2f);
            float straight = Vector3.Distance(start, car.transform.position);
            Assert.That(car.Speed, Is.GreaterThan(12f), "sedan picks up speed");
            Assert.That(car.Speed, Is.LessThanOrEqualTo(28.5f), "capped by maxSpeed");
            Assert.That(straight, Is.GreaterThan(10f), "car moved down the street");

            float yaw0 = car.transform.eulerAngles.y;
            car.scriptSteer = 1f;
            yield return new WaitForSeconds(0.6f);
            Assert.That(Mathf.Abs(Mathf.DeltaAngle(yaw0, car.transform.eulerAngles.y)), Is.GreaterThan(10f), "steering turns the car");

            car.scriptThrottle = -1f; car.scriptSteer = 0f;
            yield return new WaitForSeconds(2.5f);
            car.scriptThrottle = 0f;
            Assert.That(car.transform.position.y, Is.InRange(-0.2f, 1.0f), "car stays on the road");

            user.Exit();
            yield return null; yield return null;
            Assert.IsNull(user.Car); Assert.IsFalse(car.hasDriver);
            Assert.IsTrue(player.GetComponent<CharacterController>().enabled);
            Vector3 local = car.transform.InverseTransformPoint(player.transform.position);
            Assert.That(local.x, Is.LessThan(-1.5f), "exits out the left side");
            Assert.That(Vector3.Distance(player.transform.position, car.transform.position), Is.LessThan(4f));
        }
    }
}
