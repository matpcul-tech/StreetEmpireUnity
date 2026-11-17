using UnityEngine;

namespace StreetEmpire.City
{
    /// <summary>Marks a placed street prop (hydrant, lamp, sign, fence, tree, parked car and so on). Written by
    /// the Eastside scene builder (PropBuilder). blockId is -1 for props that belong to a street, not a block.</summary>
    public class PropMarker : MonoBehaviour
    {
        public string kind, model;
        public int blockId = -1;
    }
}
