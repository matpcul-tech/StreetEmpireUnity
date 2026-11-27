// Taking a car out of traffic: USE beside a traffic car puts the player in it, the car drives, it costs heat, the
// driver is put out on the street, and a job's own car cannot be taken.
using System.Collections;
using NUnit.Framework;
using StreetEmpire.Game;
using StreetEmpire.Player;
using StreetEmpire.Vehicles;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace StreetEmpire.Tests
{
    [PrebuildSetup(typeof(SceneSetup))]
    public class CarjackTests
    {
        [TearDown] public void Reset() { JobBoard.AutoStart = true; SideHustles.AutoStart = true; MuggerOpening.AutoStart = true; Time.timeScale = 1f; }

        [UnityTest, Timeout(300000)]
        public IEnumerator ATrafficCarCanBeTaken()
        {
            Time.timeScale = 1f; MuggerOpening.AutoStart = false; JobBoard.AutoStart = false; SideHustles.AutoStart = false;
            SceneManager.LoadScene("Eastside");
            yield return null; yield return null;
            var s = GameSession.Instance; var player = Object.FindFirstObjectByType<PlayerMotor>(); var user = player.GetComponent<VehicleUser>();
            Assert.IsNotNull(user, "the player can use cars");

            // a car in traffic on a clear stretch of street, the player beside its door with no parked or free car in reach
            JobCar theirs = null; Vector3 beside = default;
            for (int tries = 0; tries < 30 && theirs == null; tries++)
            {
                if (!s.SpawnPoint(out var at, 15f, 90f, 3f)) continue;
                var c = JobCar.Spawn(at, 8f); Assert.IsNotNull(c, "a traffic car"); c.stopped = true; c.jackable = true;
                yield return null;
                Vector3 side = c.transform.position - c.transform.right * 2.2f;
                bool busy = ParkedCars.Nearest(side, user.enterRange) != null;
                foreach (var free in Object.FindObjectsByType<CarController>(FindObjectsSortMode.None)) if (!free.hasDriver && (free.transform.position - side).magnitude < user.enterRange + 1f) busy = true;
                if (busy) { Object.Destroy(c.gameObject); yield return null; continue; }
                theirs = c; beside = side;
            }
            Assert.IsNotNull(theirs, "a clear stretch of street to stop a car on");
            var body = theirs.gameObject; Vector3 carAt = body.transform.position;
            player.Teleport(beside + Vector3.up * 0.3f, 0f);
            yield return null; yield return null;
            Assert.AreSame(theirs, Carjack.Nearest(player.transform.position, user.enterRange), "the traffic car is in reach");

            float heat0 = s.heat; int count0 = Carjack.Count;
            user.scriptUse = true;
            yield return null; yield return null;
            Assert.IsNotNull(user.Car, "in the car"); Assert.AreSame(body, user.Car.gameObject, "the same car, now his"); Assert.AreEqual("Car jacked", body.name);
            Assert.IsTrue(user.Car.hasDriver); Assert.IsNull(body.GetComponent<JobCar>(), "it left the traffic"); Assert.IsNull(body.GetComponent<CarLamps>(), "its lights are the driven car's now");
            Assert.IsNotNull(body.GetComponent<CarFx>());
            Assert.AreEqual(count0 + 1, Carjack.Count);
            Assert.That(s.heat - heat0, Is.InRange(Carjack.Heat - 0.5f, Carjack.Heat + 0.01f), "taking a car is noticed");
            StringAssert.Contains("You jacked a ride.", s.Message);
            Assert.Less(Vector3.Distance(body.transform.position, carAt), 0.5f, "taken where it stood");
            Assert.IsFalse(JobCar.All.Contains(theirs), "off the traffic list");

            // the driver is out on the street (when the town has people to be one) and gets away from the thief
            var driver = GameObject.Find("Jacked driver");
            if (TownLife.Instance != null && TownLife.Instance.templates.Length > 0)
            {
                Assert.IsNotNull(driver, "the driver is put out"); Assert.IsNull(driver.GetComponent<Townie>(), "with no errands to run");
                float d0 = Vector3.Distance(driver.transform.position, body.transform.position);
                yield return new WaitForSeconds(2f);
                Assert.Greater(Vector3.Distance(driver.transform.position, body.transform.position), d0 + 2f, "and runs");
            }

            // it drives
            Vector3 from = body.transform.position;
            user.Car.scriptThrottle = 1f; user.Car.scriptSteer = 0f;
            yield return new WaitForSeconds(2f);
            user.Car.scriptThrottle = 0f;
            Assert.Greater(Vector3.Distance(body.transform.position, from), 3f, "the taken car drives");

            // and he can get out of it again
            user.scriptUse = true;
            yield return null; yield return null;
            Assert.IsNull(user.Car, "out of the car");

            // a car that is not traffic (a job's own car) stays its driver's
            JobCar job = null;
            for (int tries = 0; tries < 30 && job == null; tries++)
            {
                if (!s.SpawnPoint(out var at, 15f, 90f, 3f)) continue;
                var c = JobCar.Spawn(at, 8f); c.stopped = true;
                yield return null;
                Vector3 side = c.transform.position - c.transform.right * 2.2f;
                bool busy = ParkedCars.Nearest(side, user.enterRange) != null;
                foreach (var free in Object.FindObjectsByType<CarController>(FindObjectsSortMode.None)) if (!free.hasDriver && (free.transform.position - side).magnitude < user.enterRange + 1f) busy = true;
                if (busy) { Object.Destroy(c.gameObject); yield return null; continue; }
                job = c; beside = side;
            }
            Assert.IsNotNull(job);
            player.Teleport(beside + Vector3.up * 0.3f, 0f);
            yield return null; yield return null;
            Assert.IsNull(Carjack.Nearest(player.transform.position, user.enterRange), "a job car is not for taking");
            user.scriptUse = true;
            yield return null; yield return null;
            Assert.IsNull(user.Car, "USE beside it does nothing");
            Assert.IsNotNull(job.GetComponent<JobCar>());
        }
    }
}
