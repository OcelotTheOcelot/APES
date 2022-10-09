using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace Verse
{
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
    public class SpaceInitializationSystemGroup : ComponentSystemGroup
    {
	}
}