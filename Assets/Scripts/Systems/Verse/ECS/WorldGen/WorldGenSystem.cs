using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Verse.WorldGen
{
	[UpdateInGroup(typeof(SpaceInitializationSystemGroup))]
	[UpdateAfter(typeof(SpaceInitializationSystem))]
	public partial class WorldGenSystem : SystemBase
	{
		private EndSimulationEntityCommandBufferSystem endSimulationEntityCommandBufferSystem;

		private EntityQuery regionQuery;
		private NativeArray<float> noise;

		protected override void OnCreate()
		{
			base.OnCreate();

			regionQuery = GetEntityQuery(
				ComponentType.ReadOnly<Region.SpatialIndex>(),
				ComponentType.ReadOnly<Region.Processing>()
			);
			regionQuery.AddSharedComponentFilter(new Region.Processing { state = Region.Processing.State.PendingGeneration });

			endSimulationEntityCommandBufferSystem = World.GetOrCreateSystemManaged<EndSimulationEntityCommandBufferSystem>();
		}

		protected override void OnStartRunning()
		{
			base.OnStartRunning();

			noise = new NativeArray<float>(Space.regionSize, Allocator.Persistent, NativeArrayOptions.ClearMemory);

			EntityCommandBuffer commandBuffer = GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(World.Unmanaged);
			var handle = new GenerateRegionJob()
			{
				terrainGenerationData = GetSingleton<TerrainGenerationData>(),

				dirtyAreas = GetComponentLookup<Chunk.DirtyArea>(),
				regionalIndexes = GetComponentLookup<Chunk.RegionalIndex>(),
				creationDatas = GetComponentLookup<Matter.Creation>(),
				atomBuffers = GetBufferLookup<Chunk.AtomBufferElement>(),
				matterColors = GetBufferLookup<Matter.ColorBufferElement>(),
				noise = noise,
				commandBuffer = commandBuffer
			}.Schedule(regionQuery, Dependency);
			handle.Complete();
			endSimulationEntityCommandBufferSystem.AddJobHandleForProducer(Dependency);
		}

		protected override void OnUpdate()
		{
		}

		public partial struct GenerateRegionJob : IJobEntity
		{
			public ComponentLookup<Chunk.DirtyArea> dirtyAreas;
			[ReadOnly]
			public ComponentLookup<Chunk.RegionalIndex> regionalIndexes;
			[ReadOnly]
			public ComponentLookup<Matter.Creation> creationDatas;
			[ReadOnly]
			public TerrainGenerationData terrainGenerationData;
			[ReadOnly]
			internal BufferLookup<Matter.ColorBufferElement> matterColors;
			public BufferLookup<Chunk.AtomBufferElement> atomBuffers;
			public EntityCommandBuffer commandBuffer;
			public NativeArray<float> noise;

			public void Execute(in Region.SpatialIndex regionIndex, in DynamicBuffer<Region.ChunkBufferElement> chunks)
			{
				int originX = regionIndex.origin.x;
				for (int x = 0; x < Space.regionSize; x++)
					noise[x] = SimplexNoise.Hill(originX + x, 100f, 20f) + SimplexNoise.Hill(originX + x, 10f, -1f) + SimplexNoise.Hill(originX + x, 500f, 50f);

				foreach (Entity chunk in chunks)
					ProcessChunk(chunk, regionIndex);
			}

			private void ProcessChunk(Entity chunk, Region.SpatialIndex regionIndex)
			{
				Coord chunkOrigin = regionalIndexes[chunk].origin;

				DynamicBuffer<Chunk.AtomBufferElement> atomBuffer = commandBuffer.SetBuffer<Chunk.AtomBufferElement>(chunk);
				foreach (Chunk.AtomBufferElement oldAtom in atomBuffers[chunk])
					atomBuffer.Add(oldAtom);

				foreach (Coord chunkCoord in Enumerators.GetSquare(Space.chunkSize))
					ProcessCell(atomBuffer, regionIndex.origin, chunkOrigin, chunkCoord);

				Chunk.DirtyArea area = dirtyAreas[chunk];

				area.MarkDirty();

				commandBuffer.SetComponent(chunk, area);
			}

			private void ProcessCell(DynamicBuffer<Chunk.AtomBufferElement> atomBuffer, Coord regionOrigin, Coord chunkOrigin, Coord chunkCoord)
			{
				Coord regionCoord = chunkOrigin + chunkCoord;
				Coord spaceCoord = regionOrigin + regionCoord;

				float additiveHeight = noise[regionCoord.x];

				if (spaceCoord.y <= terrainGenerationData.terrainHeight + additiveHeight)
					CreateAtom(atomBuffer, chunkCoord, terrainGenerationData.soilMatter);
				else if (spaceCoord.y > 512 * 1 - 64)
					CreateAtom(atomBuffer, chunkCoord, terrainGenerationData.waterMatter);
			}

			private void CreateAtom(DynamicBuffer<Chunk.AtomBufferElement> atomBuffer, Coord chunkCoord, Entity matter)
			{
				Entity atom = commandBuffer.CreateEntity(Archetypes.Atom);
				commandBuffer.SetComponent(atom, new Atom.Matter(matter));
				commandBuffer.SetComponent(atom, new Atom.Color((Color)matterColors[matter].Pick()));

				var creationData = creationDatas[matter];
				commandBuffer.SetComponent(atom, new Atom.Temperature(creationData.temperature));

				atomBuffer.SetAtom(chunkCoord, atom);
			}
		}

		[BurstCompile]
		public partial struct MarkDirtyJob : IJobEntity
		{
			public void Execute(ref Chunk.DirtyArea dirtyArea)
			{
				dirtyArea.MarkDirty();
			}
		}
	}
}