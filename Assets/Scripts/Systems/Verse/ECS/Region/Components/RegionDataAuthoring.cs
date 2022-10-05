using UnityEngine;
using Unity.Entities;

using static Verse.Region;

namespace Verse
{
	public class RegionDataAuthoring : MonoBehaviour
	{
		public class Baker : Baker<RegionDataAuthoring>
		{
			public override void Bake(RegionDataAuthoring authoring)
			{
				AddComponent(new SpatialIndex());
				AddSharedComponentManaged(new Processing());
				AddBuffer<ChunkBufferElement>();
			}
		}
	}
}
