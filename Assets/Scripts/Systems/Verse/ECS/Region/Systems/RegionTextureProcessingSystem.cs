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
		private const float defaultPixelsPerMeter = 100f;
		private EntityQuery textureQuery;

		private Color32 emptyColor;
		private float scale;
		private int regionSize;

		protected override void OnStartRunning()
		{
			base.OnStartRunning();

			textureQuery = GetEntityQuery(
				ComponentType.ReadOnly<RegionTextureParent>(),
				ComponentType.ReadOnly<SpriteRenderer>()
			);

			emptyColor = GetSingleton<SpaceData.Colors>().emptySpaceColor;

			SpaceData.Size spaceData = GetSingleton<SpaceData.Size>();
			scale = defaultPixelsPerMeter / spaceData.cellsPerMeter;

			regionSize = spaceData.regionSize;

			new CreateEmptyTextureJob
			{
				regionSize = regionSize,
				scale = scale,
				emptyColor = emptyColor
			}.Run(textureQuery);
		}

		protected override void OnUpdate()
		{
			new RebuildChunkTextureJob
			{
				emptyColor = emptyColor
			}.Run(textureQuery);
		}

		private partial struct RebuildChunkTextureJob : IJobEntity
		{
			[ReadOnly]
			public Color32 emptyColor;

			public void Execute(in RegionTextureParent owningRegion, in SpriteRenderer renderer)
			{
				Texture2D texture = renderer.sprite.texture;

				var chunks = __EntityManager.GetBuffer<RegionData.ChunkBufferElement>(owningRegion.region);

				foreach (Entity chunk in chunks)
				{
					ChunkData.DirtyArea dirtyArea = __EntityManager.GetComponentData<ChunkData.DirtyArea>(chunk);
					if (!dirtyArea.active)
						continue;

					Vector2Int fromRegionCoord = __EntityManager.GetComponentData<ChunkData.RegionalIndex>(chunk).origin + dirtyArea.from;
					Vector2Int size = dirtyArea.Size;

					var atoms = __EntityManager.GetBuffer<ChunkData.AtomBufferElement>(chunk);

					int height = dirtyArea.to.y * Space.chunkSize;

					int counter = 0;
					Color32[] colors = new Color32[size.Area()];
					for (
						int rowShift = dirtyArea.from.y * Space.chunkSize;
						rowShift < height;
						rowShift += Space.chunkSize
					)
						for (int x = dirtyArea.from.x; x < dirtyArea.to.x; x++)
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

				return __EntityManager.GetComponentData<AtomData.Color>(atom).color;
			}
		}

		public partial struct CreateEmptyTextureJob : IJobEntity
		{
			[ReadOnly]
			public int regionSize;
			
			[ReadOnly]
			public Color32 emptyColor;
			
			[ReadOnly]
			public float scale;

			public void Execute(in SpriteRenderer renderer, ref Scale scale)
			{
				renderer.sprite = Sprite.Create(
					GenerateEmptyTexture(regionSize, regionSize, emptyColor),
					new Rect(0, 0, regionSize, regionSize),
					Vector2.zero
				);

				scale.Value *= this.scale;
			}
		}

		public partial struct ProcessChunkJob : IJobEntity
		{
			[ReadOnly]
			public Color32 emptyColor;

			public Texture2D texture;

			public void Execute(
				ref ChunkData.DirtyArea dirtyArea,
				in ChunkData.RegionalIndex regionalIndex,
				in DynamicBuffer<ChunkData.AtomBufferElement> atoms 
			)
			{
				if (!dirtyArea.active)
					return;

				Vector2Int fromRegionCoord = regionalIndex.origin + dirtyArea.from;
				Vector2Int size = dirtyArea.Size;

				Debug.Log($"Rebuilding area {dirtyArea.from}-{dirtyArea.to}");

				int counter = 0;
				int height = dirtyArea.to.y * Space.chunkSize;

				Color32[] colors = new Color32[size.Area()];

				for (
					int rowShift = dirtyArea.from.y * Space.chunkSize;
					rowShift < height;
					rowShift += Space.chunkSize
				)
					for (int x = dirtyArea.from.x; x < dirtyArea.to.x; x++)
						colors[counter++] = GetColorOf(atoms[rowShift + x]);
				texture.SetPixels32(
					fromRegionCoord.x, fromRegionCoord.y, size.x, size.y,
					colors
				);
			}

			private Color32 GetColorOf(Entity atom)
			{
				if (atom == Entity.Null)
					return emptyColor;

				return World.DefaultGameObjectInjectionWorld.EntityManager.GetComponentData<AtomData.Color>(atom).color;
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