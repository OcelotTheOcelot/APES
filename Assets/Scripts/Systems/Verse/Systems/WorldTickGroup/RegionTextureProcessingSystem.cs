using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Transforms;
using System.Linq;
using UnityEngine;
using Unity.Burst;
using System.Runtime.InteropServices;
using Unity.Jobs;
using System;
using Unity.Collections.LowLevel.Unsafe;

namespace Verse
{
	[UpdateInGroup(typeof(WorldTickSystemGroup))]
	[UpdateBefore(typeof(AtomPhysicsSystem))]
	public partial class RegionTextureProcessingSystem : SystemBase
	{
		private readonly static Pixel deferredColor = new(255, 0, 255, 255);

		private NativeArray<Pixel> sliceHolder;

        private EntityQuery textureQuery;

		private Color32 emptyColor;
		private Pixel emptyColorBurst;

		protected override void OnCreate()
		{
			base.OnCreate();
			RequireForUpdate<Space.Tag>();

			sliceHolder = new(Space.totalCellsInRegion, Allocator.Persistent);
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
			int regionsToUpdate = textureQuery.CalculateEntityCount();
			NativeArray<Pixel> pixelData = new(regionsToUpdate * Space.totalCellsInRegion, Allocator.Persistent);

			new GetPixelDataJob { outputData = pixelData }.Run(textureQuery);
			new RebuildChunkTextureJob
			{
				emptyColor = emptyColorBurst,
				atomColors = GetComponentLookup<Atom.Color>(isReadOnly: true),
				dirtyAreas = GetComponentLookup<Chunk.DirtyArea>(isReadOnly: true),
				regionalIndexes = GetComponentLookup<Chunk.RegionalIndex>(isReadOnly: true),
				chunkBuffers = GetBufferLookup<Region.ChunkBufferElement>(isReadOnly: true),
				atomBuffers = GetBufferLookup<Chunk.AtomBufferElement>(isReadOnly: true),

				pixelData = pixelData
			}.Run(textureQuery);
			new LoadPixelDataJob
			{
				inputData = pixelData,
                sliceHolder = sliceHolder
            }.Run(textureQuery);
		}

		private partial struct GetPixelDataJob : IJobEntity
		{
			public NativeArray<Pixel> outputData;

            public void Execute([ReadOnly] in SpriteRenderer renderer, [EntityInQueryIndex] int regionIndex)
			{
				Texture2D texture = renderer.sprite.texture;

				NativeArray<Pixel> pixels = texture.GetRawTextureData<Pixel>();

				int firstIndex = regionIndex * Space.totalCellsInRegion;
				for (int i = 0; i < Space.totalCellsInRegion; i++)
					outputData[firstIndex + i] = pixels[i];
            }
		}

		private partial struct LoadPixelDataJob : IJobEntity
		{
			public NativeArray<Pixel> inputData;
			public NativeArray<Pixel> sliceHolder;

            public void Execute([ReadOnly] in SpriteRenderer renderer, [EntityInQueryIndex] int index)
			{
				Texture2D texture = renderer.sprite.texture;

				inputData.Slice(index * Space.totalCellsInRegion, Space.totalCellsInRegion).CopyTo(sliceHolder);

                texture.LoadRawTextureData(sliceHolder);
				texture.Apply();
			}
		}

		[BurstCompile]
		private partial struct RebuildChunkTextureJob : IJobEntity
		{
			[ReadOnly]
			public Pixel emptyColor;

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
			public NativeArray<Pixel> pixelData;

			public void Execute(
				[ReadOnly] in RegionTexture.OwningRegion owningRegion,
				[EntityInQueryIndex] int index
			)
			{
				int queryOffset = index * Space.totalCellsInRegion;

                var chunks = chunkBuffers[owningRegion.region];
				foreach (Entity chunk in chunks)
				{
					Chunk.DirtyArea dirtyArea = dirtyAreas[chunk];
					if (!dirtyArea.active)
						continue;

					NativeArray<Pixel> data = pixelData;

					Coord regionalOrigin = regionalIndexes[chunk].origin;
					int regionalOriginOffset = regionalOrigin.y * Space.regionSize + regionalOrigin.x;

					var atoms = atomBuffers[chunk];
					int chunkHeight = dirtyArea.to.y * Space.chunkSize;
					int regionRowShift = dirtyArea.from.y * Space.regionSize;
					for (int chunkRowShift = dirtyArea.from.y * Space.chunkSize; chunkRowShift <= chunkHeight; chunkRowShift += Space.chunkSize)
					{
						for (int x = dirtyArea.from.x; x <= dirtyArea.to.x; x++)
						{
							int regionalAdditiveOffset = regionRowShift + x;
							data[queryOffset + regionalOriginOffset + regionalAdditiveOffset] = GetColorOf(atoms[chunkRowShift + x]);
						}
						regionRowShift += Space.regionSize;
					}
				}
			}

			private Pixel GetColorOf(Entity atom)
			{
				if (atom == Entity.Null)
					return emptyColor;

				if (atom.Index < 0)
					return deferredColor;

				return atomColors[atom].color;
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

		[StructLayout(LayoutKind.Explicit)]
		public struct Pixel
		{
			[FieldOffset(0)]
			public int rgba;

			[FieldOffset(0)]
			public byte r;

			[FieldOffset(1)]
			public byte g;

			[FieldOffset(2)]
			public byte b;

			[FieldOffset(3)]
			public byte a;

			public Pixel(byte red, byte green, byte blue, byte alpha)
			{
				rgba = 0;

				r = red;
				g = green;
				b = blue;
				a = alpha;
			}

			public static implicit operator Color32(Pixel pixel) => new(pixel.r, pixel.g, pixel.b, pixel.a);
			public static implicit operator Pixel(Color32 color) => new(color.r, color.g, color.b, color.a);
		}
	}
}