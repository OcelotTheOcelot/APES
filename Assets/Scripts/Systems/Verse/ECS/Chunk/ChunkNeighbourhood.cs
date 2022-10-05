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

			public IEnumerator<Entity> GetEnumerator()
			{
				yield return East;
				yield return NorthEast;
				yield return North;
				yield return NorthWest;
				yield return West;
				yield return SouthWest;
				yield return South;
				yield return SouthEast;
			}

			public void MarkDirty(
				ComponentLookup<DirtyArea> dirtyAreas,
				RectInt dirtyRect,
				bool safe
			)
			{
				int chunkSize = Space.ChunkSize;

				if (dirtyRect.xMax >= chunkSize)
				{
					if (dirtyRect.yMax >= chunkSize)
						MarkDirtyIfExists(dirtyAreas, NorthEast, dirtyRect.GetShifted(-Space.ChunkSize, -Space.ChunkSize), safe);

					if (dirtyRect.yMin < 0)
						MarkDirtyIfExists(dirtyAreas, SouthEast, dirtyRect.GetShifted(-Space.ChunkSize, Space.ChunkSize), safe);

					MarkDirtyIfExists(dirtyAreas, East, dirtyRect.GetShifted(-Space.ChunkSize, 0), safe);
				}

				if (dirtyRect.xMin < 0)
				{
					if (dirtyRect.yMax >= chunkSize)
						MarkDirtyIfExists(dirtyAreas, NorthWest, dirtyRect.GetShifted(Space.ChunkSize, -Space.ChunkSize), safe);

					if (dirtyRect.yMin < 0)
						MarkDirtyIfExists(dirtyAreas, SouthWest, dirtyRect.GetShifted(Space.ChunkSize, Space.ChunkSize), safe);

					MarkDirtyIfExists(dirtyAreas, West, dirtyRect.GetShifted(Space.ChunkSize, 0), safe);
				}

				if (dirtyRect.yMax >= chunkSize)
					MarkDirtyIfExists(dirtyAreas, North, dirtyRect.GetShifted(0, -Space.ChunkSize), safe);
				if (dirtyRect.yMin < 0)
					MarkDirtyIfExists(dirtyAreas, South, dirtyRect.GetShifted(0, Space.ChunkSize), safe);
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