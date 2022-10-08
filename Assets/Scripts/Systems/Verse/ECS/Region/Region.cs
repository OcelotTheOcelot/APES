using UnityEngine;
using Unity.Entities;
using System;

namespace Verse
{
	public static class Region
	{
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
			public static int GetSortingWeight(Vector2Int index, int gridWidth) => gridWidth * index.y + index.x;
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

		public static bool CreateAtom(EntityManager dstManager, Entity region, Entity matter, Vector2Int regionCoord)
		{
			Entity chunk = GetChunk(dstManager, region, regionCoord);
			Chunk.RegionalIndex chunkIndex = dstManager.GetComponentData<Chunk.RegionalIndex>(chunk);

			return Chunk.CreateAtom(dstManager, chunk, matter, regionCoord - chunkIndex.origin);
		}

		//public static Entity GetAtom(EntityManager dstManager, Entity region, Vector2Int regionCoord)
		//{
		//	Entity chunk = GetChunk(dstManager, region, regionCoord);

		//	Vector2Int chunkCoord = regionCoord - dstManager.GetComponentData<ChunkData.RegionalIndex>(chunk).origin;

		//	return Chunk.GetAtom(dstManager, chunk, chunkCoord);
		//}

		public static Entity GetChunk(EntityManager dstManager, Entity region, Vector2Int regionCoord) =>
			dstManager.GetBuffer<ChunkBufferElement>(region).GetChunk(GetChunkPos(regionCoord));

		public static Entity GetChunk(this DynamicBuffer<ChunkBufferElement> chunks, int posX, int posY) =>
			chunks[posY * Space.chunksPerRegion + posX];
		public static Entity GetChunk(this DynamicBuffer<ChunkBufferElement> chunks, Vector2Int pos) =>
			chunks.GetChunk(pos.x, pos.y);

		public static Chunk.DirtyArea GetDirtyArea(this DynamicBuffer<ChunkBufferElement> chunks, EntityManager dstManager, Vector2Int pos) =>
			dstManager.GetComponentData<Chunk.DirtyArea>(chunks.GetChunk(pos));

		public static Chunk.DirtyArea GetDirtyArea(this DynamicBuffer<ChunkBufferElement> chunks, EntityManager dstManager, int posX, int posY) =>
			dstManager.GetComponentData<Chunk.DirtyArea>(chunks.GetChunk(posX, posY));
		
		public static Vector2Int GetChunkPos(Vector2Int regionCoord) => new(regionCoord.x / Space.chunkSize, regionCoord.y / Space.chunkSize);

		//public static void MarkDirty(this DynamicBuffer<RegionData.ChunkBufferElement> buffer)
		//{
		//    for (int i = 0; i < buffer.Length; i++)
		//        Chunk.MarkDirty(buffer);
		//}

		public static void MarkDirty(EntityManager dstManager, Entity region)
		{
			var chunks = dstManager.GetBuffer<ChunkBufferElement>(region);
			foreach (Entity chunk in chunks)
				Chunk.MarkDirty(dstManager, chunk);
		}

		public static void MarkDirty(EntityManager dstManager, Entity region, RectInt regionRect, bool safe = false)
		{
			if (safe)
				regionRect.IntersectWith(Space.regionBounds);
			
			DynamicBuffer<ChunkBufferElement> chunks = dstManager.GetBuffer<ChunkBufferElement>(region);

			Vector2Int minDist = regionRect.min.GetDivided(Space.chunkSize);
			Vector2Int maxDist = (regionRect.max - Vector2Int.one).GetDivided(Space.chunkSize);

			for (int posY = minDist.y; posY <= maxDist.y; posY++)
			{
				for (int posX = minDist.x; posX <= maxDist.x; posX++)
				{
					Entity chunk = chunks.GetChunk(posX, posY);
					Vector2Int chunkOrigin = dstManager.GetComponentData<Chunk.RegionalIndex>(chunk).origin; 
					Chunk.MarkDirty(
						dstManager, chunk,
						new RectInt(
							regionRect.min - chunkOrigin,
							regionRect.size
						)
					);
				}
			}
		}

		public static bool RemoveAtom(EntityManager dstManager, Entity region, Vector2Int regionCoord)
		{
			Entity chunk = GetChunk(dstManager, region, regionCoord);
			Chunk.RegionalIndex chunkIndex = dstManager.GetComponentData<Chunk.RegionalIndex>(chunk);

			return Chunk.RemoveAtom(dstManager, chunk, regionCoord - chunkIndex.origin);
		}

		public static Entity GetChunkByIndexNonSafe(EntityManager dstManager, Entity region, int chunkIndex) =>
			dstManager.GetBuffer<ChunkBufferElement>(region)[chunkIndex];
	}
}
