using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace Verse
{
	public static partial class Chunk
	{
		public struct Neighbourhood : IComponentData
		{
			public Entity East;
			public Entity NorthEast;
			public Entity North;
			public Entity NorthWest;
			public Entity West;
			public Entity SouthWest;
			public Entity South;
			public Entity SouthEast;

			public void MarkDirty(
				ComponentLookup<DirtyArea> dirtyAreas,
				RectInt dirtyRect,
				bool safe
			)
			{
				int chunkSize = Space.chunkSize;

				if (dirtyRect.xMax >= chunkSize)
				{
					if (dirtyRect.yMax >= chunkSize)
						MarkDirtyIfExists(dirtyAreas, NorthEast, dirtyRect.GetShifted(-Space.chunkSize, -Space.chunkSize), safe);

					if (dirtyRect.yMin < 0)
						MarkDirtyIfExists(dirtyAreas, SouthEast, dirtyRect.GetShifted(-Space.chunkSize, Space.chunkSize), safe);

					MarkDirtyIfExists(dirtyAreas, East, dirtyRect.GetShifted(-Space.chunkSize, 0), safe);
				}

				if (dirtyRect.xMin < 0)
				{
					if (dirtyRect.yMax >= chunkSize)
						MarkDirtyIfExists(dirtyAreas, NorthWest, dirtyRect.GetShifted(Space.chunkSize, -Space.chunkSize), safe);

					if (dirtyRect.yMin < 0)
						MarkDirtyIfExists(dirtyAreas, SouthWest, dirtyRect.GetShifted(Space.chunkSize, Space.chunkSize), safe);

					MarkDirtyIfExists(dirtyAreas, West, dirtyRect.GetShifted(Space.chunkSize, 0), safe);
				}

				if (dirtyRect.yMax >= chunkSize)
					MarkDirtyIfExists(dirtyAreas, North, dirtyRect.GetShifted(0, -Space.chunkSize), safe);
				if (dirtyRect.yMin < 0)
					MarkDirtyIfExists(dirtyAreas, South, dirtyRect.GetShifted(0, Space.chunkSize), safe);
			}

			private static void MarkDirtyIfExists(ComponentLookup<DirtyArea> dirtyAreas, Entity chunk, RectInt rect, bool safe = true)
			{
				if (chunk == Entity.Null)
					return;

				DirtyArea dirtyArea = dirtyAreas[chunk];
				dirtyArea.MarkDirty(rect, safe: safe);
				dirtyAreas[chunk] = dirtyArea;
			}
		}
	}
}