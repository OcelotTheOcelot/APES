using UnityEngine;
using Unity.Entities;
using static Verse.ChunkData;

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
			EntityManager entityManager,
			Neighbours neighbours,
			Vector2Int chunkCoord,
			out Entity neighbourAtom,
			out DynamicBuffer<AtomBufferElement> neighbourAtoms,
			out Vector2Int neighbourCoord
		)
		{
			int chunkSize = Space.chunkSize;
			neighbourCoord = chunkCoord;
			neighbourAtoms = atoms;
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

				return SafeGetAtomFromPotentialChunk(entityManager, neighbour, neighbourCoord, out neighbourAtom, ref atoms);
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

				return SafeGetAtomFromPotentialChunk(entityManager, neighbour, neighbourCoord, out neighbourAtom, ref atoms);
			}

			if (chunkCoord.y >= chunkSize)
			{
				neighbourCoord.y -= chunkSize;
				return SafeGetAtomFromPotentialChunk(entityManager, neighbours.North, neighbourCoord, out neighbourAtom, ref atoms);
			}
			
			if (chunkCoord.y < 0)
			{
				neighbourCoord.y += chunkSize;
				return SafeGetAtomFromPotentialChunk(entityManager, neighbours.South, neighbourCoord, out neighbourAtom, ref atoms);
			}

			neighbourAtoms = atoms;
			neighbourAtom = atoms.GetAtom(chunkCoord);
			return true;
		}

		private static bool SafeGetAtomFromPotentialChunk(
			EntityManager entityManager,
			Entity chunk,
			Vector2Int chunkCoord,
			out Entity atom,
			ref DynamicBuffer<AtomBufferElement> atoms
		)
		{
			if (chunk == Entity.Null)
			{
				atom = Entity.Null;

				return false;
			}

			atoms = entityManager.GetBuffer<AtomBufferElement>(chunk);
			atom = atoms.GetAtom(chunkCoord);
			return true;
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

		public static void Swap(
			this DynamicBuffer<AtomBufferElement> atoms, Vector2Int coordA, Vector2Int coordB
		)
		{
			Entity atom = atoms.GetAtom(coordA);
			atoms.SetAtom(coordA, atoms.GetAtom(coordB));
			atoms.SetAtom(coordB, atom);
		}
	}
}
