using System.Linq;
using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Unity.Burst;

namespace Verse
{
	[UpdateInGroup(typeof(WorldTickSystemGroup), OrderFirst = true)]
	public partial class RegionTextureProcessingSystem : SystemBase
	{
		private EntityQuery textureQuery;

		private Color32 emptyColor;

		protected override void OnStartRunning()
		{
			base.OnStartRunning();

			textureQuery = GetEntityQuery(
				ComponentType.ReadOnly<RegionTexture.OwningRegion>(),
				ComponentType.ReadOnly<SpriteRenderer>()
			);

			emptyColor = GetSingleton<Space.Colors>().emptySpaceColor;

			EntityQuery emptyTextureQuery = GetEntityQuery(
				ComponentType.ReadWrite<LocalToWorldTransform>(),
				ComponentType.ReadOnly<SpriteRenderer>()
			);
			new CreateEmptyTextureJob
			{
				regionSize = Space.RegionSize,
				emptyColor = emptyColor
			}.Run(emptyTextureQuery);
		}

		protected override void OnUpdate()
		{
			new RebuildChunkTextureJob
			{
				emptyColor = emptyColor,
				atomColors = GetComponentLookup<Atom.Color>(),
				dirtyAreas = GetComponentLookup<Chunk.DirtyArea>(),
				regionalIndexes = GetComponentLookup<Chunk.RegionalIndex>(),
				chunkBuffers = GetBufferLookup<Region.ChunkBufferElement>(),
				atomBuffers = GetBufferLookup<Chunk.AtomBufferElement>()
			}.Run(textureQuery);
		}

		[BurstCompile]
		private partial struct RebuildChunkTextureJob : IJobEntity
		{
			[ReadOnly]
			public Color32 emptyColor;

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

			public void Execute(in RegionTexture.OwningRegion owningRegion, in SpriteRenderer renderer)
			{
				Texture2D texture = renderer.sprite.texture;

				var chunks = chunkBuffers[owningRegion.region];

				foreach (Entity chunk in chunks)
				{
					Chunk.DirtyArea dirtyArea = dirtyAreas[chunk];
					if (!dirtyArea.active)
						continue;

					Vector2Int fromRegionCoord = regionalIndexes[chunk].origin + dirtyArea.from;
					Vector2Int size = dirtyArea.Size;

					var atoms = atomBuffers[chunk];

					int height = dirtyArea.to.y * Space.ChunkSize;

					int counter = 0;
					Color32[] colors = new Color32[size.Area()];
					for (
						int rowShift = dirtyArea.from.y * Space.ChunkSize;
						rowShift <= height;
						rowShift += Space.ChunkSize
					)
						for (int x = dirtyArea.from.x; x <= dirtyArea.to.x; x++)
							colors[counter++] = GetColorOf(atoms[rowShift + x]);

					texture.SetPixels32(
						fromRegionCoord.x, fromRegionCoord.y, size.x, size.y,
						colors
					);
				}

				texture.Apply();
			}

			private Color32 GetColorOf(Entity atom)
			{
				if (atom == Entity.Null)
					return emptyColor;

				if (atom.Index < 0)
					return Color.magenta;

				return atomColors[atom].color;
			}
		}

		public partial struct CreateEmptyTextureJob : IJobEntity
		{
			[ReadOnly]
			public int regionSize;
			
			[ReadOnly]
			public Color32 emptyColor;

			public void Execute(in SpriteRenderer renderer, ref LocalToWorldTransform transform)
			{
				renderer.sprite = Sprite.Create(
					GenerateEmptyTexture(regionSize, regionSize, emptyColor),
					new Rect(0, 0, regionSize, regionSize),
					Vector2.zero
				);
			}
		}


		public static Texture2D GenerateEmptyTexture(int width, int height, Color32 empty)
		{
			Texture2D texture = new(width, height, TextureFormat.ARGB32, false, false)
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