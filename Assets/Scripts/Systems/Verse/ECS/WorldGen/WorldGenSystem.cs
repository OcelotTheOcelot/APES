using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem.Interactions;

namespace Verse.WorldGen
{
	[UpdateInGroup(typeof(WorldInitializationSystemGroup))]
	[UpdateAfter(typeof(SpaceInitializationSystem))]
	public class WorldGenSystem : ComponentSystem
	{
		private float[] noise;

		private TerrainGenerationData terrainGenerationData;

		protected override void OnCreate()
		{
			base.OnCreate();
		}

		protected override void OnStartRunning()
		{
			base.OnStartRunning();

			noise = new float[Space.regionSize];

			terrainGenerationData = GetSingleton<TerrainGenerationData>();
			EntityQuery query = Entities
			.WithAll<RegionData.SpatialIndex>()
			.WithAll<RegionData.Processing>()
				.ToEntityQuery();
			query.AddSharedComponentFilter(new RegionData.Processing { state = RegionData.Processing.State.PendingGeneration });

			NativeArray<Entity> regions = query.ToEntityArray(Allocator.Temp);
			NativeArray<RegionData.SpatialIndex> chunkGridPositions = query.ToComponentDataArray<RegionData.SpatialIndex>(Allocator.Temp);

			for (int i = 0; i < regions.Length; i++)
				ProcessRegion(regions[i], chunkGridPositions[i]);

			Enabled = false;
		}

		private void ProcessRegion(Entity region, RegionData.SpatialIndex regionIndex)
		{
			var chunks = EntityManager.GetBuffer<RegionData.ChunkBufferElement>(region, isReadOnly: true).ToNativeArray(Allocator.Temp);

			int originX = regionIndex.origin.x;
			for (int x = 0; x < Space.regionSize; x++)
				noise[x] = SimplexNoise.Hill(originX + x, 100f, 20f) + SimplexNoise.Hill(originX + x, 10f, -1f) + SimplexNoise.Hill(originX + x, 500f, 50f);

			foreach (Entity chunk in chunks)
				ProcessChunk(chunk, regionIndex);
		}
		private void ProcessChunk(Entity chunk, RegionData.SpatialIndex regionIndex)
		{
			Vector2Int chunkOrigin = EntityManager.GetComponentData<ChunkData.RegionalIndex>(chunk).origin;

			foreach (Vector2Int chunkCoord in Enumerators.GetSquare(Space.chunkSize))
				ProcessCell(chunk, regionIndex.origin, chunkOrigin, chunkCoord);

			Chunk.MarkDirty(EntityManager, chunk);
		}

		public struct ProcessCellJob : IJobParallelFor
		{
			public Entity chunk;
			public EntityManager entityManager;
			public Entity matter;

			public Vector2Int chunkOrigin;
			public Vector2Int regionOrigin;

			public NativeArray<Vector2Int> chunkCoords;
			public NativeArray<float> noise;

			public float terrainHeight;

			public void Execute(int index)
			{
				Vector2Int chunkCoord = chunkCoords[index];
				Vector2Int regionCoord = chunkOrigin + chunkCoord;
				Vector2Int spaceCoord = regionOrigin + regionCoord;

				float additiveHeight = noise[regionCoord.x];

				if (spaceCoord.y <= terrainHeight + additiveHeight)
				{
					Chunk.CreateAtom(entityManager, chunk, matter, chunkCoord);
				}
			}
		}

		private void ProcessCell(Entity chunk, Vector2Int regionOrigin, Vector2Int chunkOrigin, Vector2Int chunkCoord)
		{
			Vector2Int regionCoord = chunkOrigin + chunkCoord;
			Vector2Int spaceCoord = regionOrigin + regionCoord;

			float additiveHeight = noise[regionCoord.x];

			if (spaceCoord.y <= terrainGenerationData.terrainHeight + additiveHeight)
			{
				CreateAtom(chunk, chunkCoord, terrainGenerationData.soilMatter);
			}
		}

		private void CreateAtom(Entity chunk, Vector2Int chunkCoord, Entity matter) =>
			Chunk.CreateAtom(EntityManager, chunk, matter, chunkCoord);

		protected override void OnUpdate()
		{
		}
	}
}