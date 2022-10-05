using UnityEngine;
using Unity.Entities;

using static Verse.Chunk;

namespace Verse
{
	public class ChunkDataAuthoring : MonoBehaviour
	{
		public class Baker : Baker<ChunkDataAuthoring>
		{
			public override void Bake(ChunkDataAuthoring authoring)
			{
				AddComponent(new DirtyArea());
				AddComponent(new RegionalIndex());
				AddComponent(new Chunk.SpatialIndex());
				AddComponent(new Neighbourhood());
				AddBuffer<AtomBufferElement>();
				AddSharedComponentManaged(new ProcessingBatchIndex());
			}
		}
	}
}