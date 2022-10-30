using UnityEngine;
using Unity.Entities;

using static Verse.Region;

namespace Verse
{
	public class RegionAuthoring : MonoBehaviour
	{
		public class Baker : Baker<RegionAuthoring>
		{
			public override void Bake(RegionAuthoring authoring)
			{
				AddComponent(new SpatialIndex());
				AddSharedComponentManaged(new Processing());
				AddBuffer<ChunkBufferElement>();
			}
		}
	}
}
