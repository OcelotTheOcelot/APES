using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Entities.UniversalDelegates;
using UnityEngine;
using Verse;
using static UnityEditor.Experimental.AssetDatabaseExperimental.AssetDatabaseCounters;

namespace Verse
{
	[DisallowMultipleComponent]
	public class ChunkData : MonoBehaviour, IConvertGameObjectToEntity
	{
		public void Convert(Entity chunk, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
		{
			dstManager.AddComponentData(chunk, new DirtyArea());
			dstManager.AddComponentData(chunk, new RegionalIndex());
			dstManager.AddComponentData(chunk, new SpatialIndex());
			dstManager.AddComponentData(chunk, new Neighbours());
			dstManager.AddBuffer<AtomBufferElement>(chunk);
			dstManager.AddSharedComponentData(chunk, new ProcessingBatchIndex());
		}

		public struct RegionalIndex : IComponentData
		{
			public Vector2Int index;
			public Vector2Int origin;

			public RegionalIndex(Vector2Int position)
			{
				index = position;
				origin = position * Space.chunkSize;
			}
		}

		public struct SpatialIndex : IComponentData
		{
			public Vector2Int origin;

			public SpatialIndex(RegionData.SpatialIndex regionIndex, RegionalIndex index)
			{
				origin = regionIndex.origin + index.origin;
			}
		}

		public struct Region : IComponentData
		{
			public Entity region;
		}

		public struct Neighbours : IComponentData
		{
			public Entity East;
			public Entity NorthEast;
			public Entity North;
			public Entity NorthWest;
			public Entity West;
			public Entity SouthWest;
			public Entity South;
			public Entity SouthEast;
		}

		public struct DirtyArea : IComponentData
		{
			public Entity chunk;

			public bool active;

			public Vector2Int from;
			public Vector2Int to;

			private static readonly Vector2Int maxSize = new Vector2Int(Space.chunkSize, Space.chunkSize);

			//public DirtyArea(Entity chunk)
			//{
			//	this.chunk = chunk;

			//	active = false;
			//	from = to = -Vector2Int.one;
			//}

			public Vector2Int Size => to - from;
			public int Area => (to.x - from.x) * (to.y - from.y);

			public IEnumerable<Vector2Int> AllCoords
			{
				get
				{
					if (!active)
						yield break;

					for (int y = from.y; y < to.y; y++)
						for (int x = from.x; x < to.x; x++)
							yield return new Vector2Int(x, y);
				}
			}

			public void AddCoord(Vector2Int regionCoord, bool safe = false)
			{
				if (safe)
				{
					regionCoord = Vector2Int.Max(regionCoord, maxSize);
					regionCoord = Vector2Int.Min(regionCoord, Vector2Int.zero);
				}

				if (!active)
				{
					from = regionCoord;
					to = regionCoord;

					active = true;

					return;
				}

				from = Vector2Int.Min(from, regionCoord);
				to = Vector2Int.Max(to, regionCoord);
			}

			public void AddCoord(int x, int y)
			{
				if (!active)
				{
					from.Set(x, y);
					to.Set(x, y);

					active = true;

					return;
				}

				from.Set(Mathf.Min(from.x, x), Mathf.Min(from.y, y));
				to.Set(Mathf.Max(to.x, x), Mathf.Max(to.y, y));
			}

			public void MarkDirty()
			{
				active = true;

				from = Vector2Int.zero;
				to = maxSize;
			}

			public void MarkDirty(RectInt chunkRect, bool safe)
			{
				if (safe && !chunkRect.IntersectWith(Space.chunkBounds))
					return;

				AddCoord(chunkRect.min);
				AddCoord(chunkRect.max);
			}

			public override string ToString() => active ? $"{from}–{to}" : "[empty]";

			public IEnumerable<Vector2Int> GetSnake(int tick)
			{
				int oddity = (tick + from.y) & 1;

				for (int y = from.y; y < to.y; y++)
				{
					if (oddity == 1)
						for (int x = from.x; x < to.x; x++)
							yield return new Vector2Int(x, y);
					else
						for (int x = to.x - 1; x >= from.x; x--)
							yield return new Vector2Int(x, y);

					oddity ^= 1;
				}
			}
		}

		[InternalBufferCapacity(64)]
		public struct AtomBufferElement : IBufferElementData
		{
			public Entity atom;

			public static implicit operator Entity(AtomBufferElement bufferElement) => bufferElement.atom;
			public static implicit operator AtomBufferElement(Entity atom) => new()
			{
				atom = atom
			};
		}

		/// <summary>
		/// Determines order in which clusters should be processed.
		/// Clusters with the same number can be safely processed simultaneously.
		/// I'm lazy so I'll just illustrate:
		/// 
		/// 0 1 0 1 0 1
		/// 2 3 2 3 2 3
		/// 0 1 0 1 0 1
		/// 2 3 2 3 2 3
		/// 
		/// So that chunks with same index can be processed at the same time.
		/// </summary>
		public struct ProcessingBatchIndex : ISharedComponentData
		{
			public int batchIndex;

			public ProcessingBatchIndex(int batchIndex)
			{
				this.batchIndex = batchIndex;
			}

			public ProcessingBatchIndex(Vector2Int gridPos)
			{
				batchIndex = GetIndexFromGridPos(gridPos);
			}

			public static int GetIndexFromGridPos(Vector2Int gridPos) => GetIndexFromGridPos(gridPos.x & 0b1, gridPos.y & 0b1);
			public static int GetIndexFromGridPos(int gridXOddity, int gridYOddity) => (gridYOddity << 1) + gridXOddity;
		}
	}
}