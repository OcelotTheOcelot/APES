using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Verse
{
	public static partial class Chunk
	{
		public struct DirtyArea : IComponentData
		{
			public bool active;
			public bool frameProtection;
            public CoordRect rect;

			public Coord From => rect.min;
			public Coord To => rect.max;

			private static readonly Coord maxSize = new(Space.chunkSize - 1, Space.chunkSize - 1);

			public DirtyArea(CoordRect chunkRect)
			{
                active = chunkRect.IntersectWith(Space.chunkBounds);
				rect = chunkRect;
				frameProtection = false;
            }

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
					Coord.Min(From, chunkCoord),
					Coord.Max(To, chunkCoord)
				);
			}

			public void MarkDirty(int x, int y)
			{
				if (!active)
				{
                    rect.min.Set(x, y);
                    rect.max.Set(x, y);

					active = true;

					return;
				}
                
                rect.min.Set(math.min(rect.min.x, x), math.min(rect.min.y, y));
                rect.max.Set(math.max(rect.max.x, x), math.max(rect.max.y, y));
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

				rect.StretchCombineWith(chunkRect);
			}

			public override string ToString() => active ? $"{rect}" : "[empty]";
		}

		public struct ScheduledDirtyRect : IComponentData
		{
			public bool active;
			public CoordRect rect;

			public static implicit operator CoordRect(ScheduledDirtyRect rect) => rect.rect;
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
	}
}