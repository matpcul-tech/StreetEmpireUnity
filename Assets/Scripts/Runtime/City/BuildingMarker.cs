using UnityEngine;

namespace StreetEmpire.City
{
    /// <summary>Marks a placed kit building. Written by the Eastside scene builder.</summary>
    public class BuildingMarker : MonoBehaviour
    {
        public int blockId, index;
        public string kind, model;
        public int wash;      // houses: which stucco wash (SceneBuilder.HouseTint) it got
    }
}
