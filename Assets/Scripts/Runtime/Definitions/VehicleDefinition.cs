// Unity needs one ScriptableObject per file, named like the class. Built by Street Empire > Build Data Assets.
using StreetEmpire.Sim.Data;
using UnityEngine;

namespace StreetEmpire.Definitions
{
    public class VehicleDefinition : ScriptableObject { public VehicleData data; public GameObject prefab; }
}
