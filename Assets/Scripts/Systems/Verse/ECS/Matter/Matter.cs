using UnityEngine;
using Unity.Entities;

namespace Verse
{
	public static class Matter
	{
		public static MatterData.PhysicProperties GetPhysicalProperties(EntityManager dstManager, Entity matter) =>
			dstManager.GetComponentData<MatterData.PhysicProperties>(matter);

		public static MatterState GetState(EntityManager dstManager, Entity matter) =>
			dstManager.GetComponentData<MatterData.State>(matter).state;
	}
}
