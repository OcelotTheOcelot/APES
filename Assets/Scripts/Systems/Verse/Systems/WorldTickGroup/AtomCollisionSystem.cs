using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace Verse
{
	[UpdateInGroup(typeof(WorldTickSystemGroup))]
	[UpdateAfter(typeof(AtomPhysicsSystem))]
	public partial class AtomCollisionSystem : SystemBase
	{


		protected override void OnUpdate()
		{
		}
	}
}