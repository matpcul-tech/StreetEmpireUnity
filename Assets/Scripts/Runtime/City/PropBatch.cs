using System;
using UnityEngine;

namespace StreetEmpire.City
{
    /// <summary>One street prop model as drawn by PropInstancer: its merged mesh (one submesh per material),
    /// an optional lower detail mesh with the same submesh order, where it switches and stops drawing, and
    /// every placed copy as a matrix.</summary>
    [Serializable]
    public class PropBatch
    {
        public string name;
        public Mesh lod0, lod1;
        public Material[] materials;
        public float lod1Distance = 25f, cullDistance = 80f;
        public bool castShadows = true;
        /// <summary>Copies farther than this draw without a shadow.</summary>
        public float shadowDistance = 35f;
        public Matrix4x4[] matrices;
        public Vector3 center; public float radius = 1f;   // bounding sphere in mesh space
        /// <summary>Rendering layers of the drawn copies: 1 for props, 3 for the parked cars (layer 2 is what the
        /// night fill light reaches, DayCycle.nightFill).</summary>
        public uint renderingLayers = 1;
    }
}
