using UnityEngine;

namespace StreetEmpire.City
{
    /// <summary>Marks a placed kit building. Written by the Eastside scene builder.</summary>
    public class BuildingMarker : MonoBehaviour
    {
        public int blockId, index;
        public string kind, model;
        public int wash;      // houses: which stucco wash (SceneBuilder.HouseTint) it got
        /// <summary>v75c: the graffiti piece on its side wall (-1 none; 0 EASTSIDE, 1 LOCALS, 2 RED K, 3 EMPIRE), its center and outward normal.</summary>
        public int graffiti = -1;
        public Vector3 graffitiAt, graffitiNormal;
    }
}
