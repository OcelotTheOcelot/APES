using UnityEngine;
using Unity.Entities;

namespace Verse
{
	public static class Atom
	{
		public static Entity GetMatter(EntityManager dstManager, Entity atom) =>
			dstManager.GetComponentData<AtomData.Matter>(atom).matterPrefab;
	}
}
