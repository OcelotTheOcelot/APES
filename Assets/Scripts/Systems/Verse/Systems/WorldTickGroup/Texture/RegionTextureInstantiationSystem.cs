using Unity.Entities;
using Unity.Collections;
using System.Linq;
using UnityEngine;
using Unity.Burst;
using Unity.Jobs;
using Unity.Collections.LowLevel.Unsafe;
using static Verse.RegionTexture;

namespace Verse
{
	[UpdateInGroup(typeof(SpaceInitializationSystemGroup))]
	[UpdateAfter(typeof(WorldGen.WorldGenSystem))]
	public partial class RegionTextureInstantiationSystem : RegionTextureProcessingSystem
	{
		private EndSimulationEntityCommandBufferSystem endSimulationEntityCommandBufferSystem;

		protected override void OnCreate()
		{
			base.OnCreate();

			endSimulationEntityCommandBufferSystem = World.GetOrCreateSystemManaged<EndSimulationEntityCommandBufferSystem>();
		}

		protected override void InitializeQuery()
		{
			textureQuery = GetEntityQuery(
				ComponentType.ReadOnly<SpriteRenderer>(),
				ComponentType.ReadOnly<OwningRegion>(),
				ComponentType.ReadWrite<Processing>()
			);
			textureQuery.AddSharedComponentFilter(new Processing(false));
		}

		protected override void OnUpdate()
		{
			EntityCommandBuffer.ParallelWriter commandBuffer = GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(World.Unmanaged).AsParallelWriter();

			int regionsAmount = textureQuery.CalculateEntityCount();
			pixelData.Length = regionsAmount;

			for (int i = 0; i < regionsAmount; i++)
				pixelData[i] = new NativeArray<AtomColor>(Space.totalCellsInRegion, Allocator.Persistent);

			Dependency = new BuildInitialChunkTextureJob
			{
				atomColors = GetComponentLookup<Atom.Color>(isReadOnly: true),
				regionalIndexes = GetComponentLookup<Chunk.RegionalIndex>(isReadOnly: true),
				chunkBuffers = GetBufferLookup<Region.ChunkBufferElement>(isReadOnly: true),
				atomBuffers = GetBufferLookup<Chunk.AtomBufferElement>(isReadOnly: true),

				commandBuffer = commandBuffer,
				pixelData = pixelData
			}.ScheduleParallel(textureQuery, Dependency);
			new CreateTextureJob
			{
				inputData = pixelData
			}.Run(textureQuery);

			endSimulationEntityCommandBufferSystem.AddJobHandleForProducer(Dependency);
		}

		[BurstCompile]
		protected partial struct BuildInitialChunkTextureJob : IJobEntity
		{
			[ReadOnly]
			public ComponentLookup<Atom.Color> atomColors;
			[ReadOnly]
			public ComponentLookup<Chunk.RegionalIndex> regionalIndexes;
			[ReadOnly]
			public BufferLookup<Region.ChunkBufferElement> chunkBuffers;
			[ReadOnly]
			public BufferLookup<Chunk.AtomBufferElement> atomBuffers;

			[WriteOnly]
			public UnsafeList<NativeArray<AtomColor>> pixelData;

			public EntityCommandBuffer.ParallelWriter commandBuffer;

			public void Execute(
				Entity regTex,
				[ReadOnly] in OwningRegion owningRegion,
				[EntityInQueryIndex] int queryIndex
			)
			{
				NativeArray<AtomColor> data = pixelData[queryIndex];

				DynamicBuffer<Region.ChunkBufferElement> chunks = chunkBuffers[owningRegion.region];
				foreach (Entity chunk in chunks)
				{
					Coord regionalOrigin = regionalIndexes[chunk].origin;
					int regionalOriginOffset = regionalOrigin.y * Space.regionSize + regionalOrigin.x;

					DynamicBuffer<Chunk.AtomBufferElement> atoms = atomBuffers[chunk];
					int chunkHeight = Space.totalCellsInChunk;
					int regionRowShift = 0;
					for (int chunkRowShift = 0; chunkRowShift < chunkHeight; chunkRowShift += Space.chunkSize)
					{
						for (int x = 0; x < Space.chunkSize; x++)
						{
							data[regionalOriginOffset + regionRowShift + x] = atomColors.GetColorOf(atoms[chunkRowShift + x]);
						}
						regionRowShift += Space.regionSize;
					}
				}

				commandBuffer.SetSharedComponent(queryIndex, regTex, new Processing(true));
			}
		}

		public partial struct CreateTextureJob : IJobEntity
		{
			[WriteOnly]
			public UnsafeList<NativeArray<AtomColor>> inputData;

			public void Execute(in SpriteRenderer renderer, [EntityInQueryIndex] int queryIndex)
			{
				int regionSize = Space.regionSize;
				Texture2D texture = new(regionSize, regionSize, TextureFormat.RGBA32, false, false)
				{
					filterMode = FilterMode.Point
				};

				renderer.sprite = Sprite.Create(
					texture,
					new Rect(0, 0, regionSize, regionSize),
					Vector2.zero
				);

				texture.LoadRawTextureData(inputData[queryIndex]);
				texture.Apply();
			}
		}

		public static Texture2D GenerateEmptyTexture(int width, int height, Color32 empty)
		{
			Texture2D texture = new(width, height, TextureFormat.RGBA32, false, false)
			{
				filterMode = FilterMode.Point
			};

			Color32[] colors = Enumerable.Repeat(empty, width * height).ToArray();
			texture.SetPixels32(0, 0, width, height, colors);
			texture.Apply();

			return texture;
		}
	}
}