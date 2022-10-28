using UnityEngine;
using Unity.Entities;
using static Verse.Chunk;
using System.Runtime.CompilerServices;
using Unity.Mathematics;

namespace Verse
{
	public static class AtomBufferExtention
	{
		public static Entity GetAtom(this DynamicBuffer<AtomBufferElement> atoms, int chunkCoordX, int chunkCoordY) => atoms[chunkCoordY * Space.chunkSize + chunkCoordX];
		public static Entity GetAtom(this DynamicBuffer<AtomBufferElement> atoms, Coord chunkCoord) => atoms.GetAtom(chunkCoord.x, chunkCoord.y);
		public static Entity GetAtom(this DynamicBuffer<AtomBufferElement> atoms, int2 chunkCoord) => atoms.GetAtom(chunkCoord.x, chunkCoord.y);

		public static void SetAtom(this DynamicBuffer<AtomBufferElement> atoms, int chunkCoordX, int chunkCoordY, Entity atom) => atoms[chunkCoordY * Space.chunkSize + chunkCoordX] = atom;
		public static void SetAtom(this DynamicBuffer<AtomBufferElement> atoms, Coord chunkCoord, Entity atom) => atoms.SetAtom(chunkCoord.x, chunkCoord.y, atom);
		public static void SetAtom(this DynamicBuffer<AtomBufferElement> atoms, int2 chunkCoord, Entity atom) => atoms.SetAtom(chunkCoord.x, chunkCoord.y, atom);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool GetAtomNeighbourFallback(
			this DynamicBuffer<AtomBufferElement> atoms,
			BufferLookup<AtomBufferElement> atomBuffers,
			Neighbourhood neighbours,
			Coord chunkCoord,
			out Entity neighbourAtom,
			out DynamicBuffer<AtomBufferElement> neighbourAtoms,
			out Coord neighbourCoord
		)
		{
			neighbourAtoms = atoms;
			neighbourCoord = chunkCoord;
			Entity neighbour;

			if (chunkCoord.x >= Space.chunkSize)
			{
				neighbourCoord.x -= Space.chunkSize;
				if (chunkCoord.y >= Space.chunkSize)
				{
					neighbourCoord.y -= Space.chunkSize;
					neighbour = neighbours.NorthEast;
				}
				else if (chunkCoord.y < 0)
				{
					neighbourCoord.y += Space.chunkSize;
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
				neighbourCoord.x += Space.chunkSize;
				if (chunkCoord.y >= Space.chunkSize)
				{
					neighbourCoord.y -= Space.chunkSize;
					neighbour = neighbours.NorthWest;
				}
				else if (chunkCoord.y < 0)
				{
					neighbourCoord.y += Space.chunkSize;
					neighbour = neighbours.SouthWest;
				}
				else
				{
					neighbour = neighbours.West;
				}

				return SafeGetAtomFromPotentialChunk(atomBuffers, neighbour, neighbourCoord, ref neighbourAtoms, out neighbourAtom);
			}

			if (chunkCoord.y >= Space.chunkSize)
			{
				neighbourCoord.y -= Space.chunkSize;
				return SafeGetAtomFromPotentialChunk(atomBuffers, neighbours.North, neighbourCoord, ref neighbourAtoms, out neighbourAtom);
			}
			if (chunkCoord.y < 0)
			{
				neighbourCoord.y += Space.chunkSize;
				return SafeGetAtomFromPotentialChunk(atomBuffers, neighbours.South, neighbourCoord, ref neighbourAtoms, out neighbourAtom);
			}

			neighbourAtom = atoms.GetAtom(chunkCoord);
			return true;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool GetAtomNeighbourFallback(
			this DynamicBuffer<AtomBufferElement> atoms, BufferLookup<AtomBufferElement> atomBuffers,
			Neighbourhood neighbours, Coord chunkCoord, out Entity atom
		)
		{
			Coord neighbourCoord = chunkCoord;
			Entity neighbour;

			if (chunkCoord.x >= Space.chunkSize)
			{
				neighbourCoord.x -= Space.chunkSize;
				if (chunkCoord.y >= Space.chunkSize)
				{
					neighbourCoord.y -= Space.chunkSize;
					neighbour = neighbours.NorthEast;
				}
				else if (chunkCoord.y < 0)
				{
					neighbourCoord.y += Space.chunkSize;
					neighbour = neighbours.SouthEast;
				}
				else
				{
					neighbour = neighbours.East;
				}

				return SafeGetAtomFromPotentialChunk(atomBuffers, neighbour, neighbourCoord, out atom);
			}
			if (chunkCoord.x < 0)
			{
				neighbourCoord.x += Space.chunkSize;
				if (chunkCoord.y >= Space.chunkSize)
				{
					neighbourCoord.y -= Space.chunkSize;
					neighbour = neighbours.NorthWest;
				}
				else if (chunkCoord.y < 0)
				{
					neighbourCoord.y += Space.chunkSize;
					neighbour = neighbours.SouthWest;
				}
				else
				{
					neighbour = neighbours.West;
				}

				return SafeGetAtomFromPotentialChunk(atomBuffers, neighbour, neighbourCoord, out atom);
			}

			if (chunkCoord.y >= Space.chunkSize)
			{
				neighbourCoord.y -= Space.chunkSize;
				return SafeGetAtomFromPotentialChunk(atomBuffers, neighbours.North, neighbourCoord, out atom);
			}
			if (chunkCoord.y < 0)
			{
				neighbourCoord.y += Space.chunkSize;
				return SafeGetAtomFromPotentialChunk(atomBuffers, neighbours.South, neighbourCoord, out atom);
			}

			atom = atoms.GetAtom(chunkCoord);
			return true;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static bool SafeGetAtomFromPotentialChunk(
			BufferLookup<AtomBufferElement> atomBuffers,
			Entity chunk,
			Coord chunkCoord,
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

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static bool SafeGetAtomFromPotentialChunk(
			BufferLookup<AtomBufferElement> atomBuffers, Entity chunk, Coord chunkCoord, out Entity atom
		)
		{
			if (chunk == Entity.Null)
			{
				atom = Entity.Null;
				return false;
			}
			
			atom = atomBuffers[chunk].GetAtom(chunkCoord);
			return true;
		}

		public static void Swap(
			this DynamicBuffer<AtomBufferElement> atoms,
			Coord coordA, Coord coordB
		)
		{
			Entity atom = atoms.GetAtom(coordA);
			atoms.SetAtom(coordA, atoms.GetAtom(coordB));
			atoms.SetAtom(coordB, atom);
		}

		public static void Swap(
			DynamicBuffer<AtomBufferElement> atomsA, Coord coordA,
			DynamicBuffer<AtomBufferElement> atomsB, Coord coordB
		)
		{
			Entity atom = atomsA.GetAtom(coordA);
			atomsA.SetAtom(coordA, atomsB.GetAtom(coordB));
			atomsB.SetAtom(coordB, atom);
		}

        public static void Swap(
            this DynamicBuffer<AtomBufferElement> atomsA, int indexA,
            DynamicBuffer<AtomBufferElement> atomsB, Coord coordB
        )
        {
            Entity atom = atomsA[indexA];
			atomsA[indexA] = atomsB.GetAtom(coordB);
            atomsB.SetAtom(coordB, atom);
        }

        //public static void 
    }
}
