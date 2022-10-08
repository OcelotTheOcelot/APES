using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Verse
{
	public static class Space
	{
		public readonly static int regionSize = 512;
		public readonly static float regionPerCell = 1f / regionSize;
		public readonly static RectInt regionBounds = new(0, 0, regionSize - 1, regionSize - 1);


		public readonly static int chunkSize = 64;
		public readonly static float chunkPerCell = 1f / chunkSize;
		public readonly static int chunksPerRegion = regionSize / chunkSize;
		public readonly static RectInt chunkBounds = new(0, 0, chunkSize - 1, chunkSize - 1);

		public readonly static float cellsPerMeter = 20f;
		public readonly static float metersPerCell = 1f / cellsPerMeter;

		public struct Tag : IComponentData { }

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

		public static bool RemoveAtom(EntityManager dstManager, Entity space, Vector2Int spaceCoord)
		{
			if (!GetRegion(dstManager, space, spaceCoord, out Entity region))
				return false;

			Region.SpatialIndex regionIndex = dstManager.GetComponentData<Region.SpatialIndex>(region);

			return Region.RemoveAtom(dstManager, region, spaceCoord - regionIndex.origin);
		}
		
		public static bool CreateAtom(EntityManager dstManager, Entity space, Entity matter, Vector2Int spaceCoord)
		{
			if (!GetRegion(dstManager, space, spaceCoord, out Entity region))
				return false;

			Region.SpatialIndex regionIndex = dstManager.GetComponentData<Region.SpatialIndex>(region);

			return Region.CreateAtom(dstManager, region, matter, spaceCoord - regionIndex.origin);
		}

		public static bool GetRegionByIndex(EntityManager dstManager, Entity space, Vector2Int regionIndex, out Entity outputRegion)
		{
			Region.SpatialIndex targetRegionIndex = new(regionIndex);

			int weight = targetRegionIndex.GetSortingWeight(
				dstManager.GetComponentData<Bounds>(space).spaceGridBounds.width
			);

			var regions = dstManager.GetBuffer<RegionBufferElement>(space);
			foreach (var region in regions)
			{
				if (dstManager.GetComponentData<Region.SpatialIndex>(region).index != targetRegionIndex.index)
					continue;

				outputRegion = region;
				return true;
			}

			outputRegion = Entity.Null;
			return false;
		}

		public static bool GetRegion(EntityManager dstManager, Entity space, Vector2Int spaceCoord, out Entity outputRegion) =>
			GetRegionByIndex(dstManager, space, GetRegionIndex(spaceCoord), out outputRegion);

		public static Vector2Int GetRegionIndex(Vector2Int spaceCoord) =>
			new(
				Mathf.FloorToInt(spaceCoord.x * regionPerCell),
				Mathf.FloorToInt(spaceCoord.y * regionPerCell)
			);

		public static Vector2Int WorldToSpace(LocalToWorldTransform transform, float3 worldPos)
		{
			float3 localPos = worldPos - transform.Value.Position;
			localPos *= cellsPerMeter;

			return new Vector2Int(
				Mathf.FloorToInt(localPos.x),
				Mathf.FloorToInt(localPos.y)
			);
		}

		public static void MarkDirty(EntityManager dstManager, Entity space, RectInt spaceRect, bool safe)
		{
			for (int regPosY = spaceRect.yMin / regionSize; regPosY <= ((spaceRect.yMax - 1) / regionSize); regPosY++)
			{
				for (int regPosX = spaceRect.xMin / regionSize; regPosX <= ((spaceRect.xMax - 1) / regionSize); regPosX++)
				{
					if (!GetRegionByIndex(dstManager, space, new Vector2Int(regPosX, regPosY), out Entity region))
						continue;

					Region.MarkDirty(
						dstManager, region,
						new RectInt(
							SpaceToRegion(dstManager, region, spaceRect.position),
							spaceRect.size
						),
						safe: safe
					);
				}
			}
		}

		public static Vector2Int SpaceToRegion(EntityManager dstManager, Entity region, Vector2Int spaceRect) =>
			SpaceToRegion(dstManager.GetComponentData<Region.SpatialIndex>(region), spaceRect);

		public static Vector2Int SpaceToRegion(Region.SpatialIndex regionIndex, Vector2Int spaceCoord) =>
			spaceCoord - regionIndex.origin;
	}
}