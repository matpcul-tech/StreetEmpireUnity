// Unity needs one ScriptableObject per file, named like the class. Built by Street Empire > Build Data Assets.
using StreetEmpire.Sim.Data;
using UnityEngine;

namespace StreetEmpire.Definitions
{
    public class ContactDefinition : ScriptableObject { public ContactData data; public GameObject characterPrefab; }
}
