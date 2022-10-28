using System;
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
                ref DirtyArea thisChunkArea,
                ComponentLookup<DirtyArea> dirtyAreas,
                CoordRect dirtyRect,
                bool safe
            )
            {
                thisChunkArea.MarkDirty(dirtyRect, safe);
                MarkDirty(dirtyAreas, dirtyRect, safe: safe);
            }

            public void MarkDirty(
				ComponentLookup<DirtyArea> dirtyAreas,
				CoordRect dirtyRect,
				bool safe
			)
			{
				if (dirtyRect.xMax >= Space.chunkSize)
				{
					if (dirtyRect.yMax >= Space.chunkSize)
						MarkDirtyIfExists(dirtyAreas, NorthEast, dirtyRect.GetShifted(-Space.chunkSize, -Space.chunkSize), safe);

					if (dirtyRect.yMin < 0)
						MarkDirtyIfExists(dirtyAreas, SouthEast, dirtyRect.GetShifted(-Space.chunkSize, Space.chunkSize), safe);

					MarkDirtyIfExists(dirtyAreas, East, dirtyRect.GetShifted(-Space.chunkSize, 0), safe);
				}

				if (dirtyRect.xMin < 0)
				{
					if (dirtyRect.yMax >= Space.chunkSize)
						MarkDirtyIfExists(dirtyAreas, NorthWest, dirtyRect.GetShifted(Space.chunkSize, -Space.chunkSize), safe);

					if (dirtyRect.yMin < 0)
						MarkDirtyIfExists(dirtyAreas, SouthWest, dirtyRect.GetShifted(Space.chunkSize, Space.chunkSize), safe);

					MarkDirtyIfExists(dirtyAreas, West, dirtyRect.GetShifted(Space.chunkSize, 0), safe);
				}

				if (dirtyRect.yMax >= Space.chunkSize)
					MarkDirtyIfExists(dirtyAreas, North, dirtyRect.GetShifted(0, -Space.chunkSize), safe);
				if (dirtyRect.yMin < 0)
					MarkDirtyIfExists(dirtyAreas, South, dirtyRect.GetShifted(0, Space.chunkSize), safe);
			}

			private static void MarkDirtyIfExists(ComponentLookup<DirtyArea> dirtyAreas, Entity chunk, CoordRect rect, bool safe = true)
			{
				if (chunk == Entity.Null)
					return;

				DirtyArea dirtyArea = dirtyAreas[chunk];
				dirtyArea.MarkDirty(rect, safe: safe);
				dirtyAreas[chunk] = dirtyArea;
			}

            public bool GetNeighbourAtCoord(Coord coord, out Entity neighbour, out Coord neighbourCoord)
            {
                neighbourCoord = coord;

                if (coord.x >= Space.chunkSize)
                {
					neighbourCoord.x -= Space.chunkSize;
                    if (coord.y >= Space.chunkSize)
					{
                        neighbourCoord.y -= Space.chunkSize;
                        neighbour =  NorthEast;
						return true;
                    }

                    if (coord.y < 0)
					{
                        neighbourCoord.y += Space.chunkSize;
                        neighbour =  SouthEast;
						return true;
                    }

                    neighbour =  East;
					return true;
                }

                if (coord.x < 0)
                {
					neighbourCoord.x += Space.chunkSize;
                    if (coord.y >= Space.chunkSize)
					{
                        neighbourCoord.y -= Space.chunkSize;
                        neighbour =  NorthWest;
						return true;
                    }

                    if (coord.y < 0)
					{
                        neighbourCoord.y += Space.chunkSize;
                        neighbour =  SouthWest;
						return true;
                    }

                    neighbour =  West;
                    return true;
                }

                if (coord.y >= Space.chunkSize)
				{
					neighbourCoord.y -= Space.chunkSize;
                    neighbour =  North;
                    return true;
                }
                if (coord.y < 0)
				{
					neighbourCoord.y += Space.chunkSize;
                    neighbour =  South;
                    return true;
                }

				neighbour = Entity.Null;
                return false;
            }
        }
	}
}