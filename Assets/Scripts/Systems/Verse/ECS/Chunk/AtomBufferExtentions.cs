using UnityEngine;
using Unity.Entities;
using static Verse.Chunk;

namespace Verse
{
	public static class AtomBufferExtention
	{
		public static Entity GetAtom(this DynamicBuffer<AtomBufferElement> atoms, int chunkCoordX, int chunkCoordY) => atoms[chunkCoordY * Space.chunkSize + chunkCoordX];
		public static Entity GetAtom(this DynamicBuffer<AtomBufferElement> atoms, Vector2Int chunkCoord) => atoms.GetAtom(chunkCoord.x, chunkCoord.y);

		public static void SetAtom(this DynamicBuffer<AtomBufferElement> atoms, int chunkCoordX, int chunkCoordY, Entity atom) => atoms[chunkCoordY * Space.chunkSize + chunkCoordX] = atom;
		public static void SetAtom(this DynamicBuffer<AtomBufferElement> atoms, Vector2Int chunkCoord, Entity atom) => atoms.SetAtom(chunkCoord.x, chunkCoord.y, atom);

		public static bool GetAtomNeighbourFallback(
			this DynamicBuffer<AtomBufferElement> atoms,
			BufferLookup<AtomBufferElement> atomBuffers,
			Neighbourhood neighbours,
			Vector2Int chunkCoord,
			out Entity neighbourAtom,
			out DynamicBuffer<AtomBufferElement> neighbourAtoms,
			out Vector2Int neighbourCoord
		)
		{
			int chunkSize = Space.chunkSize;
			neighbourAtoms = atoms;
			neighbourCoord = chunkCoord;
			Entity neighbour;

			if (chunkCoord.x >= chunkSize)
			{
				neighbourCoord.x = chunkCoord.x - chunkSize;
				if (chunkCoord.y >= chunkSize)
				{
					neighbourCoord.y -= chunkSize;
					neighbour = neighbours.NorthEast;
				}
				else if (chunkCoord.y < 0)
				{
					neighbourCoord.y += chunkSize;
					neighbour = neighbours.SouthEast;
				}
				else
				{
					neighbour = neighbours.East;
				}

				return SafeGetAtomFromPotentialChunk(atomBuffers, neighbour, neighbourCoord, ref neighbourAtoms, out neighbourAtom);
			}

			if (chunkCoord.x < 0)
			{
				neighbourCoord.x = chunkCoord.x + chunkSize;
				if (chunkCoord.y >= chunkSize)
				{
					neighbourCoord.y -= chunkSize;
					neighbour = neighbours.NorthWest;
				}
				else if (chunkCoord.y < 0)
				{
					neighbourCoord.y += chunkSize;
					neighbour = neighbours.SouthWest;
				}
				else
				{
					neighbour = neighbours.West;
				}

				return SafeGetAtomFromPotentialChunk(atomBuffers, neighbour, neighbourCoord, ref neighbourAtoms, out neighbourAtom);
			}

			if (chunkCoord.y >= chunkSize)
			{
				neighbourCoord.y -= chunkSize;
				return SafeGetAtomFromPotentialChunk(atomBuffers, neighbours.North, neighbourCoord, ref neighbourAtoms, out neighbourAtom);
			}
			
			if (chunkCoord.y < 0)
			{
				neighbourCoord.y += chunkSize;
				return SafeGetAtomFromPotentialChunk(atomBuffers, neighbours.South, neighbourCoord, ref neighbourAtoms, out neighbourAtom);
			}

			neighbourAtom = atoms.GetAtom(chunkCoord);
			return true;
		}

		private static bool SafeGetAtomFromPotentialChunk(
			BufferLookup<AtomBufferElement> atomBuffers,
			Entity chunk,
			Vector2Int chunkCoord,
			ref DynamicBuffer<AtomBufferElement> atoms,
			out Entity atom
		)
		{
			if (chunk == Entity.Null)
			{
				atom = Entity.Null;

				return false;
			}

			atoms = atomBuffers[chunk];
			atom = atoms.GetAtom(chunkCoord);
			return true;
		}

		public static void Swap(
			this DynamicBuffer<AtomBufferElement> atoms,
			Vector2Int coordA, Vector2Int coordB
		)
		{
			Entity atom = atoms.GetAtom(coordA);
			atoms.SetAtom(coordA, atoms.GetAtom(coordB));
			atoms.SetAtom(coordB, atom);
		}

		public static void Swap(
			DynamicBuffer<AtomBufferElement> atomsA, Vector2Int coordA,
			DynamicBuffer<AtomBufferElement> atomsB, Vector2Int coordB
		)
		{
			Entity atom = atomsA.GetAtom(coordA);
			atomsA.SetAtom(coordA, atomsB.GetAtom(coordB));
			atomsB.SetAtom(coordB, atom);
		}
	}
}
