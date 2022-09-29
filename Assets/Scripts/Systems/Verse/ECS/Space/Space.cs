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
		// DEPRECATED
		public static readonly int regionSize = 512;
		public static readonly float regionPerCell = 1f / regionSize;

		public static readonly RectInt regionBounds = new(0, 0, regionSize, regionSize);

		// DEPRECATED
		public static readonly int chunkSize = 64;
		public static readonly float chunkPerCell = 1f / chunkSize;

		// DEPRECATED
		public static readonly int chunksPerRegion = regionSize / chunkSize;
		public static readonly RectInt chunkBounds = new(0, 0, chunkSize, chunkSize);

		public static bool CreateAtom(EntityManager dstManager, Entity space, Entity matter, Vector2Int spaceCoord)
		{
			if (!GetRegion(dstManager, space, spaceCoord, out Entity region))
				return false;

			RegionData.SpatialIndex regionIndex = dstManager.GetComponentData<RegionData.SpatialIndex>(region);

			return Region.CreateAtom(dstManager, region, matter, spaceCoord - regionIndex.origin);
		}

		public static bool GetRegionByIndex(EntityManager dstManager, Entity space, Vector2Int regionIndex, out Entity outputRegion)
		{
			RegionData.SpatialIndex targetRegionIndex = new(regionIndex);

			int weight = targetRegionIndex.GetSortingWeight(
				dstManager.GetComponentData<SpaceData.Bounds>(space).spaceGridBounds.width
			);

			var regions = dstManager.GetBuffer<SpaceData.RegionBufferElement>(space);
			foreach (var region in regions)
			{
				if (dstManager.GetComponentData<RegionData.SpatialIndex>(region).index != targetRegionIndex.index)
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

		public static Vector2Int WorldToSpace(Translation translation, SpaceData.Size sizeData, float3 worldPos)
		{
			float3 localPos = worldPos - translation.Value;
			localPos *= sizeData.cellsPerMeter;

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
			SpaceToRegion(dstManager.GetComponentData<RegionData.SpatialIndex>(region), spaceRect);

		public static Vector2Int SpaceToRegion(RegionData.SpatialIndex regionIndex, Vector2Int spaceCoord) =>
			spaceCoord - regionIndex.origin;
	}
}