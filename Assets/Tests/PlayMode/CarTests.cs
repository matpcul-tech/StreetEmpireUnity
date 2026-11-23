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
        // every parked vehicle can be taken: its drawn copy leaves the instancer and the same body drives
        [UnityTest]
        public IEnumerator ParkedVehiclesCanBeDriven()
        {
            StreetEmpire.Game.MuggerOpening.AutoStart = false; SceneManager.LoadScene("Eastside");
            yield return null; yield return null;
            var player = Object.FindFirstObjectByType<PlayerMotor>();
            var user = player.GetComponent<VehicleUser>();
            var inst = Object.FindFirstObjectByType<StreetEmpire.City.PropInstancer>();
            int Copies() { int n = 0; foreach (var b in inst.batches) if (b != null && b.name != null && b.name.StartsWith("car-") && b.matrices != null) n += b.matrices.Length; return n; }
            foreach (string kind in new[] { "hatchback", "city-bus" })
            {
                StreetEmpire.City.PropMarker mark = null;
                foreach (var m in Object.FindObjectsByType<StreetEmpire.City.PropMarker>(FindObjectsSortMode.None)) if (m.kind == "parked car" && m.model == kind) { mark = m; break; }
                if (mark == null) continue;
                int before = Copies(); var go = mark.gameObject; Vector3 start = go.transform.position;
                var side = go.GetComponent<BoxCollider>();
                player.Teleport(start + go.transform.right * (side.size.x * 0.5f + 1.2f), 0f);
                yield return null;
                Assert.IsTrue(user.TryEnter(), "took the parked " + kind);
                Assert.AreSame(go, user.Car.gameObject, "the parked " + kind + " is the car");
                Assert.AreEqual(before - 1, Copies(), "its drawn copy left the instancer");
                Assert.IsNotNull(go.GetComponentInChildren<MeshRenderer>(), "it has a body of its own");
                user.Car.scriptThrottle = 1f; user.Car.scriptSteer = 0f;
                for (int i = 0; i < 90; i++) yield return new WaitForFixedUpdate();
                Assert.Greater(Vector3.Distance(go.transform.position, start), 1.5f, "the " + kind + " drives");
                user.Car.scriptThrottle = 0f; user.Exit();
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator EnterDriveSteerAndExitSedan()
        {
            StreetEmpire.Game.MuggerOpening.AutoStart = false; SceneManager.LoadScene("Eastside");
            yield return null; yield return null;
            var car = Object.FindFirstObjectByType<CarController>();
            var player = Object.FindFirstObjectByType<PlayerMotor>();
            var user = player.GetComponent<VehicleUser>();
            Assert.IsNotNull(car, "sedan in scene"); Assert.IsNotNull(user, "VehicleUser on player");
            Assert.AreEqual(28f, car.stats.maxSpeed, 0.01f, "sedan maxSpeed from vehicles.json");
            // the body has to be drawn where the car is (renderer bounds are live: the car is not static batched)
            var rs = car.GetComponentsInChildren<Renderer>();
            Assert.Greater(rs.Length, 0, "sedan meshes");
            var rb = rs[0].bounds; foreach (var r in rs) rb.Encapsulate(r.bounds);
            Assert.Less(Vector2.Distance(new Vector2(rb.center.x, rb.center.z), new Vector2(car.transform.position.x, car.transform.position.z)), 1.5f, "model sits on the car");
            Assert.That(rb.size.z, Is.InRange(4f, 5f), "kit sedan is 4.5 m long");
            Assert.That(rb.min.y - car.transform.position.y, Is.InRange(-0.1f, 0.3f), "wheels on the road");
            var box = car.GetComponent<BoxCollider>();
            Assert.Less(Vector3.Distance(box.bounds.center, rb.center), 0.5f, "collider wraps the body");
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

            // at the curb the driver's door opens onto the street: out the left side, then back in
            user.Exit();
            yield return null; yield return null;
            Vector3 local = car.transform.InverseTransformPoint(player.transform.position);
            Assert.That(local.x, Is.LessThan(-1.5f), "exits out the left side");
            Assert.IsTrue(player.GetComponent<CharacterController>().enabled);
            Assert.IsTrue(user.TryEnter(), "back in from the door");

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
            // after a drive the left door may face a wall; then state.js uses the right side
            Assert.That(Mathf.Abs(car.transform.InverseTransformPoint(player.transform.position).x), Is.InRange(1.5f, 3.2f), "out beside the car");
        }
    }
}
