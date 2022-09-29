using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using static Verse.ChunkData;

namespace Verse
{
	[DisallowMultipleComponent]
	public class RegionData : MonoBehaviour, IConvertGameObjectToEntity
	{
		public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
		{
			dstManager.AddComponentData(entity, new SpatialIndex());
			dstManager.AddSharedComponentData(entity, new Processing());
			dstManager.AddBuffer<ChunkBufferElement>(entity);
		}

		public struct Processing : ISharedComponentData
		{
			public State state;

			public enum State
			{
				// Non-existing chunk without even coordinates set
				PendingInitialization = 0,

				// Chunk w/o any atoms in it, waiting to be populated by worldgen system
				PendingGeneration = 1,

                // Chunk w/o any atoms in it, waiting to be loaded from the disk or received from the server
                PendingLoading = 2,

				// Chunk that is being actively processed (usually around a player)
				Active = 3,

				// Chunk that is put on hold
				Sleeping = 4
			}
		}

		public struct SpatialIndex : IComponentData
		{
			public Vector2Int origin;
			public Vector2Int index;

			public SpatialIndex(Vector2Int spatialIndex) : this()
			{
				index = spatialIndex;
				origin = spatialIndex * Space.regionSize;
			}

			public int GetSortingWeight(int gridWidth) => gridWidth * index.y + index.x;
		}


		[InternalBufferCapacity(64)]
		public struct ChunkBufferElement : IBufferElementData
		{
			public Entity chunk;

			public static implicit operator Entity(ChunkBufferElement bufferElement) => bufferElement.chunk;
			public static implicit operator ChunkBufferElement(Entity chunk) => new ChunkBufferElement
			{
				chunk = chunk
			};
		}
	}
}
