using System.Linq;

using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Internal;

namespace Verse
{
	[DisallowMultipleComponent]
	public class SpaceData : MonoBehaviour, IConvertGameObjectToEntity
	{
		[SerializeField] private int regionSize = 512;
		[SerializeField] private int chunkSize = 64;
		[SerializeField] private float cellsPerMeter = 16f;

		[SerializeField] private Color32 emptySpaceColor;
		
		[SerializeField] private Vector2Int defaultChunkChount;
		
		public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
		{
			dstManager.AddComponentData(entity,
				new Size
				{
					regionSize = regionSize,
					chunkSize = chunkSize,
					cellsPerMeter = cellsPerMeter
				}
			);

			dstManager.AddComponentData(entity, new Colors
				{
					emptySpaceColor = emptySpaceColor
				}
			);

			dstManager.AddComponentData(entity, new Initialization { regionCount = defaultChunkChount});

			dstManager.AddComponentData(entity, new Bounds());

			dstManager.AddBuffer<RegionBufferElement>(entity);
		}

		public struct Size : IComponentData
		{
			public int regionSize;
			public int chunkSize;
			public float cellsPerMeter;
		}

		public struct Bounds : IComponentData
		{
			public RectInt spaceGridBounds;
		}

		public struct Colors : IComponentData
		{
			public Color32 emptySpaceColor;
		}

		public struct Initialization : IComponentData
		{
			public Vector2Int regionCount;
		}

		[InternalBufferCapacity(16)]
		public struct RegionBufferElement : IBufferElementData
		{
			public Entity region;

			public static implicit operator Entity(RegionBufferElement bufferElement) => bufferElement.region;
			public static implicit operator RegionBufferElement(Entity region) => new()
			{
                region = region
            };
		}
	}
}