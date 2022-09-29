using UnityEngine;
using Unity.Entities;
using System;

using static Verse.RegionData;

namespace Verse
{
	public static class Region
	{
		public static bool CreateAtom(EntityManager dstManager, Entity region, Entity matter, Vector2Int regionCoord)
		{
			Entity chunk = GetChunk(dstManager, region, regionCoord);
			ChunkData.RegionalIndex chunkIndex = dstManager.GetComponentData<ChunkData.RegionalIndex>(chunk);

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

		public static ChunkData.DirtyArea GetDirtyArea(this DynamicBuffer<ChunkBufferElement> chunks, EntityManager dstManager, Vector2Int pos) =>
			dstManager.GetComponentData<ChunkData.DirtyArea>(chunks.GetChunk(pos));

		public static ChunkData.DirtyArea GetDirtyArea(this DynamicBuffer<ChunkBufferElement> chunks, EntityManager dstManager, int posX, int posY) =>
			dstManager.GetComponentData<ChunkData.DirtyArea>(chunks.GetChunk(posX, posY));
		
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
				regionRect.IntersectWithNonSafe(Space.regionBounds);
			
			DynamicBuffer<ChunkBufferElement> chunks = dstManager.GetBuffer<ChunkBufferElement>(region);

			Vector2Int minDist = regionRect.min.GetDivided(Space.chunkSize);
			Vector2Int maxDist = (regionRect.max - Vector2Int.one).GetDivided(Space.chunkSize);

			for (int posY = minDist.y; posY <= maxDist.y; posY++)
			{
				for (int posX = minDist.x; posX <= maxDist.x; posX++)
				{
					Entity chunk = chunks.GetChunk(posX, posY);
					Vector2Int chunkOrigin = dstManager.GetComponentData<ChunkData.RegionalIndex>(chunk).origin; 
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
	}
}
