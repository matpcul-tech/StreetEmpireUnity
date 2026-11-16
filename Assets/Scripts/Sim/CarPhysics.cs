// driveCar() from state.js, in Unity's convention (yaw clockwise from +Z, so a positive steer turns right).
// The JS works in mirrored three.js space and subtracts the turn; mirroring X flips that sign, nothing else.
using System;
using StreetEmpire.Sim.Data;

namespace StreetEmpire.Sim
{
    public class CarState
    {
        public float x, z, yaw;     // position and the way the body points
        public float heading;       // the way it actually travels (mh in the JS); differs when sliding
        public float v, steer, slip;
        public bool skid, headingSet;
    }

    public static class CarPhysics
    {
        public static void Drive(CarState c, VehicleData t, float throttle, float steerIn, bool handbrake, float dt)
        {
            float wb = t.wheelbase > 0 ? t.wheelbase : 2.7f;
            if (!c.headingSet || Math.Abs(c.v) < 1) { c.heading = c.yaw; c.headingSet = true; }
            float sp = Math.Abs(c.v) / t.maxSpeed;
            float lockAngle = 0.6f * (1 - Math.Min(0.5f, sp * 0.55f)) * (handbrake ? 1.25f : 1);
            c.steer += (steerIn * lockAngle - c.steer) * Math.Min(1, dt * (Math.Abs(steerIn) > 0.05f ? 7 : 10));
            bool braking = throttle * c.v < 0;
            c.v += throttle * (braking ? 34 : c.v < 0 ? 10 : t.accel) * dt;
            c.v *= 1 - (handbrake ? 0.7f : Math.Abs(throttle) < 0.05f ? 0.9f : 0.25f) * dt;
            c.v = Math.Max(-9, Math.Min(t.maxSpeed, c.v));
            c.yaw += c.v / wb * (float)Math.Tan(c.steer) * dt * (handbrake ? 1.6f : 1);
            float grip = handbrake ? 1.1f : 6.5f - sp * 2;
            c.heading += AngDiff(c.yaw, c.heading) * Math.Min(1, dt * grip);
            c.slip = Math.Abs(AngDiff(c.yaw, c.heading));
            c.skid = (c.slip > 0.14f && Math.Abs(c.v) > 6) || (braking && Math.Abs(c.v) > 9) || (throttle > 0.5f && c.v > 0 && c.v < 5 && sp < 0.15f);
            if (!handbrake && c.slip > 0.6f) c.v *= 1 - 1.2f * dt; // spinning out scrubs speed
            c.x += (float)Math.Sin(c.heading) * c.v * dt;
            c.z += (float)Math.Cos(c.heading) * c.v * dt;
        }

        /// <summary>collideCarBuildings response: (nx, nz) is the wall normal. Returns the damage the JS applies.</summary>
        public static float HitWall(CarState c, float nx, float nz)
        {
            float k = Math.Max(0.3f, Math.Abs((float)Math.Sin(c.heading) * nx + (float)Math.Cos(c.heading) * nz));
            float n = Math.Abs(c.v) * k;
            c.v *= n > 10 ? -0.25f : 1 - 0.15f * k;
            c.heading = c.yaw;
            return n * 1.6f;
        }

        public static float AngDiff(float a, float b)
        {
            float d = (a - b) % (2 * MathF.PI);
            if (d > MathF.PI) d -= 2 * MathF.PI; else if (d < -MathF.PI) d += 2 * MathF.PI;
            return d;
        }
    }
}
