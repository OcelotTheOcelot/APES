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
			public CoordRect rect;

			public Coord from => rect.min;
			public Coord to => rect.max;

			private static readonly Coord maxSize = new(Space.chunkSize - 1, Space.chunkSize - 1);
			public Coord Size => rect.Size;
			public int Area => rect.Area;

			public void MarkDirty(Coord chunkCoord, bool safe = false)
			{
				if (safe)
					chunkCoord.Clamp(Coord.zero, maxSize);

				if (!active)
				{
					rect.min = rect.max = chunkCoord;
					active = true;

					return;
				}

				rect.Set(
					Coord.Min(from, chunkCoord),
					Coord.Max(to, chunkCoord)
				);
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
				rect = new(Coord.zero, maxSize);
			}

			public void MarkDirty(CoordRect chunkRect, bool safe)
			{
				if (safe && !chunkRect.IntersectWith(Space.chunkBounds))
					return;

				if (!active)
				{
					rect = chunkRect;
					active = true;

					return;
				}

				rect.min = Coord.Min(rect.min, chunkRect.min);
				rect.max = Coord.Max(rect.max, chunkRect.max);
			}

			public override string ToString() => active ? $"{rect}" : "[empty]";
		}

		public static void MarkDirty(EntityManager dstManager, Entity chunk, CoordRect rect, bool safe = true)
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

		public static void MarkDirtyNeighbourFallback(EntityManager dstManager, Entity chunk, Neighbourhood neighbours, CoordRect dirtyRect)
		{
			MarkDirty(dstManager, chunk, dirtyRect, safe: true);

			throw new System.NotImplementedException();
		}
	}
}