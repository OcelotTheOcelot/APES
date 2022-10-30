using Unity.Entities;
using Unity.Collections;
using System.Linq;
using UnityEngine;
using Unity.Burst;
using Unity.Jobs;
using Unity.Collections.LowLevel.Unsafe;

using static Verse.RegionTexture;
using System;

namespace Verse
{
	public abstract partial class RegionTextureProcessingSystem : SystemBase
	{
        protected EntityQuery textureQuery;
        protected UnsafeList<NativeArray<AtomColor>> pixelData;

        protected override void OnCreate()
        {
            base.OnCreate();

            RequireForUpdate<Space.Tag>();

            pixelData = new(1, Allocator.Persistent);
            InitializeQuery();
        }

        protected virtual void InitializeQuery()
        {
            textureQuery = GetEntityQuery(
                ComponentType.ReadOnly<SpriteRenderer>(),
                ComponentType.ReadOnly<OwningRegion>(),
                ComponentType.ReadOnly<Processing>()
            );
            textureQuery.AddSharedComponentFilter(new Processing(true));
        }
    }
    

    [UpdateInGroup(typeof(VerseTickSystemGroup))]
	[UpdateAfter(typeof(AtomPhysicsSystem))]
	public partial class RegionTextureUpdateSystem : RegionTextureProcessingSystem
    {

		protected override void OnUpdate()
		{
			// For consistent indexing, it's important that we use the same query (I guess)
			int regionsAmount = textureQuery.CalculateEntityCount();
			pixelData.Length = regionsAmount;

            new GetPixelDataJob
			{
				outputData = pixelData
			}.Run(textureQuery);
			new RebuildChunkTextureJob
			{
				atomColors = GetComponentLookup<Atom.Color>(isReadOnly: true),
				dirtyAreas = GetComponentLookup<Chunk.DirtyArea>(isReadOnly: true),
				regionalIndexes = GetComponentLookup<Chunk.RegionalIndex>(isReadOnly: true),
				chunkBuffers = GetBufferLookup<Region.ChunkBufferElement>(isReadOnly: true),
				atomBuffers = GetBufferLookup<Chunk.AtomBufferElement>(isReadOnly: true),

				pixelData = pixelData
			}.ScheduleParallel(textureQuery);
			new LoadPixelDataJob
			{
				inputData = pixelData
			}.Run(textureQuery);
		}


		protected partial struct GetPixelDataJob : IJobEntity
		{
			[WriteOnly]
			public UnsafeList<NativeArray<AtomColor>> outputData;

			public void Execute([ReadOnly] in SpriteRenderer renderer, [EntityInQueryIndex] int queryIndex)
			{
				Texture2D texture = renderer.sprite.texture;

				outputData[queryIndex] = texture.GetRawTextureData<AtomColor>();
			}
		}

		[BurstCompile]
		protected partial struct RebuildChunkTextureJob : IJobEntity
		{
			[ReadOnly]
			public ComponentLookup<Atom.Color> atomColors;
			[ReadOnly]
			public ComponentLookup<Chunk.DirtyArea> dirtyAreas;
			[ReadOnly]
			public ComponentLookup<Chunk.RegionalIndex> regionalIndexes;
			[ReadOnly]
			public BufferLookup<Region.ChunkBufferElement> chunkBuffers;
			[ReadOnly]
			public BufferLookup<Chunk.AtomBufferElement> atomBuffers;

			[WriteOnly]
			public UnsafeList<NativeArray<AtomColor>> pixelData;

			public void Execute(
				[ReadOnly] in OwningRegion owningRegion,
				[EntityInQueryIndex] int queryIndex
			)
			{
				NativeArray<AtomColor> data = pixelData[queryIndex];

				var chunks = chunkBuffers[owningRegion.region];
				foreach (Entity chunk in chunks)
				{
					Chunk.DirtyArea dirtyArea = dirtyAreas[chunk];
					if (!dirtyArea.active)
						continue;

					Coord regionalOrigin = regionalIndexes[chunk].origin;
					int regionalOriginOffset = regionalOrigin.y * Space.regionSize + regionalOrigin.x;

					var atoms = atomBuffers[chunk];
					int chunkHeight = dirtyArea.To.y * Space.chunkSize;
					int regionRowShift = dirtyArea.From.y * Space.regionSize;
					for (int chunkRowShift = dirtyArea.From.y * Space.chunkSize; chunkRowShift <= chunkHeight; chunkRowShift += Space.chunkSize)
					{
						for (int x = dirtyArea.From.x; x <= dirtyArea.To.x; x++)
						{
							int regionalAdditiveOffset = regionRowShift + x;
							data[regionalOriginOffset + regionalAdditiveOffset] = atomColors.GetColorOf(atoms[chunkRowShift + x]);
						}
						regionRowShift += Space.regionSize;
					}
				}
			}
        }

        protected partial struct LoadPixelDataJob : IJobEntity
        {
            [ReadOnly]
            public UnsafeList<NativeArray<AtomColor>> inputData;

            public void Execute([ReadOnly] in SpriteRenderer renderer, [EntityInQueryIndex] int queryIndex)
            {
                Texture2D texture = renderer.sprite.texture;

                texture.LoadRawTextureData(inputData[queryIndex]);
                texture.Apply();
            }
        }
    }
}