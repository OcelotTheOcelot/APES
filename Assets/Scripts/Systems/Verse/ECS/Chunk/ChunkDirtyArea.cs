using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace Verse
{
	public static partial class Chunk
	{
		public struct DirtyArea : IComponentData
		{
			public bool active;

			public Vector2Int from;
			public Vector2Int to;

			private static readonly Vector2Int maxSize = new(Space.ChunkSize - 1, Space.ChunkSize - 1);

			public Vector2Int Size => to - from + Vector2Int.one;

			public IEnumerable<Vector2Int> AllCoords
			{
				get
				{
					if (!active)
						yield break;

					for (int y = from.y; y <= to.y; y++)
						for (int x = from.x; x <= to.x; x++)
							yield return new Vector2Int(x, y);
				}
			}

			public void MarkDirty(Vector2Int chunkCoord, bool safe = false)
			{
				if (safe)
					chunkCoord.Clamp(Vector2Int.zero, maxSize);

				if (!active)
				{
					from = chunkCoord;
					to = chunkCoord;

					active = true;

					return;
				}

				from = Vector2Int.Min(from, chunkCoord);
				to = Vector2Int.Max(to, chunkCoord);
			}

			public void MarkDirty(int x, int y)
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
				if (safe && !chunkRect.IntersectWith(Space.ChunkBounds))
					return;

				if (!active)
				{
					from = chunkRect.min;
					to = chunkRect.max;

					active = true;

					return;
				}

				from = Vector2Int.Min(from, chunkRect.min);
				to = Vector2Int.Max(to, chunkRect.max);
			}

			public override string ToString() => active ? $"[{from}–{to}]" : "[empty]";

			public IEnumerable<Vector2Int> GetSnake(int tick)
			{
				int oddity = (tick + from.y) & 1;

				for (int y = from.y; y <= to.y; y++)
				{
					if (oddity == 1)
						for (int x = from.x; x <= to.x; x++)
							yield return new Vector2Int(x, y);
					else
						for (int x = to.x - 1; x >= from.x; x--)
							yield return new Vector2Int(x, y);

					oddity ^= 1;
				}
			}
		}

		public static void MarkDirty(EntityManager dstManager, Entity chunk, RectInt rect, bool safe = true)
		{
			DirtyArea area = dstManager.GetComponentData<DirtyArea>(chunk);
			area.MarkDirty(rect, safe: safe);
			dstManager.SetComponentData(chunk, area);
		}

		public static void MarkDirty(EntityManager dstManager, Entity chunk)
		{
			DirtyArea area = dstManager.GetComponentData<DirtyArea>(chunk);
			area.MarkDirty();
			dstManager.SetComponentData(chunk, area);
		}

		public static void MarkDirtyNeighbourFallback(EntityManager dstManager, Entity chunk, Neighbourhood neighbours, RectInt dirtyRect)
		{
			MarkDirty(dstManager, chunk, dirtyRect, safe: true);

			throw new System.NotImplementedException();
		}
	}
}