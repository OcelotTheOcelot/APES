using Unity.Entities;
using Unity.Collections;
using System.Linq;
using UnityEngine;
using Unity.Burst;
using Unity.Jobs;
using Unity.Collections.LowLevel.Unsafe;

namespace Verse
{
	[UpdateInGroup(typeof(WorldTickSystemGroup))]
	[UpdateBefore(typeof(AtomPhysicsSystem))]
	public partial class RegionTextureProcessingSystem : SystemBase
	{
		private readonly static AtomColor deferredColor = new(255, 0, 255, 255);
		
		private EntityQuery textureQuery;
		private UnsafeList<NativeArray<AtomColor>> pixelData;

		private Color32 emptyColor;
		private AtomColor emptyColorBurst;

		protected override void OnCreate()
		{
			base.OnCreate();
			RequireForUpdate<Space.Tag>();

			pixelData = new(1, Allocator.Persistent);
		}

		protected override void OnStartRunning()
		{
			base.OnStartRunning();

			textureQuery = GetEntityQuery(ComponentType.ReadOnly<SpriteRenderer>(), ComponentType.ReadOnly<RegionTexture.OwningRegion>());

			emptyColor = GetSingleton<Space.Colors>().emptySpaceColor;
			emptyColorBurst = new(emptyColor.r, emptyColor.g, emptyColor.b, emptyColor.a);

			EntityQuery textureInitializationQuery = GetEntityQuery(
				ComponentType.ReadOnly<SpriteRenderer>()
			);
			new CreateEmptyTextureJob
			{
				emptyColor = emptyColor
			}.Run(textureInitializationQuery);
		}

		protected override void OnUpdate()
		{
			// For consistent indexing, it's important that we use the ame query (I guess)
			int regionsAmount = textureQuery.CalculateEntityCount();
			pixelData.Length = regionsAmount;

			new GetPixelDataJob
			{
				outputData = pixelData
			}.Run(textureQuery);
			new RebuildChunkTextureJob
			{
				emptyColor = emptyColorBurst,
				atomColors = GetComponentLookup<Atom.Color>(isReadOnly: true),
				dirtyAreas = GetComponentLookup<Chunk.DirtyArea>(isReadOnly: true),
				regionalIndexes = GetComponentLookup<Chunk.RegionalIndex>(isReadOnly: true),
				chunkBuffers = GetBufferLookup<Region.ChunkBufferElement>(isReadOnly: true),
				atomBuffers = GetBufferLookup<Chunk.AtomBufferElement>(isReadOnly: true),

				pixelData = pixelData
			}.Schedule(textureQuery, Dependency).Complete();
			new LoadPixelDataJob
			{
				inputData = pixelData
			}.Run(textureQuery);
		}

		private partial struct GetPixelDataJob : IJobEntity
		{
			[WriteOnly]
			public UnsafeList<NativeArray<AtomColor>> outputData;

			public void Execute([ReadOnly] in SpriteRenderer renderer, [EntityInQueryIndex] int queryIndex)
			{
				Texture2D texture = renderer.sprite.texture;

				outputData[queryIndex] = texture.GetRawTextureData<AtomColor>();
			}
		}

		private partial struct LoadPixelDataJob : IJobEntity
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

		[BurstCompile]
		private partial struct RebuildChunkTextureJob : IJobEntity
		{
			[ReadOnly]
			public AtomColor emptyColor;

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
				[ReadOnly] in RegionTexture.OwningRegion owningRegion,
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
							data[regionalOriginOffset + regionalAdditiveOffset] = GetColorOf(atoms[chunkRowShift + x]);
						}
						regionRowShift += Space.regionSize;
					}
				}
			}

			private AtomColor GetColorOf(Entity atom)
			{
				if (atom == Entity.Null)
					return emptyColor;

				if (atom.Index < 0)
					return deferredColor;

				return atomColors[atom].value;
			}
		}

		public partial struct CreateEmptyTextureJob : IJobEntity
		{
			[ReadOnly]
			public Color32 emptyColor;

			public void Execute(in SpriteRenderer renderer)
			{
				int regionSize = Space.regionSize;
				renderer.sprite = Sprite.Create(
					GenerateEmptyTexture(regionSize, regionSize, emptyColor),
					new Rect(0, 0, regionSize, regionSize),
					Vector2.zero
				);
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