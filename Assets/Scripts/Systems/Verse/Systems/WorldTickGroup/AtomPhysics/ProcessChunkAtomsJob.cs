using Unity.Entities;

using Unity.Jobs;
using Unity.Collections;
using Unity.Burst;

using Unity.Mathematics;
using System.Runtime.CompilerServices;
using UnityEngine;
using static Verse.Chunk;
using static Verse.AtomPhysics;
using System;
using Apes.UI;
using Apes.Random;

namespace Verse
{
	public partial class AtomPhysicsSystem : SystemBase
	{
		[BurstCompile]
		public partial struct ProcessChunkAtomsJob : IJobEntity
		{
			[ReadOnly]
			public int tick;
			private int hashKeyAddition;

			[ReadOnly]
			public ComponentLookup<Atom.Matter> lookupMatter;
			[ReadOnly]
			public ComponentLookup<Matter.AtomState> lookupState;
			[ReadOnly]
			public ComponentLookup<Matter.PhysicProperties> lookupProps;

			[NativeDisableParallelForRestriction]
			public BufferLookup<AtomBufferElement> lookupAtoms;
			[NativeDisableParallelForRestriction]
			public ComponentLookup<DirtyArea> lookupDirtyArea;

			[NativeDisableParallelForRestriction]
			private DynamicBuffer<AtomBufferElement> atoms;

			public void Execute(
				Entity chunk,
				[ReadOnly] in Neighbourhood neighbours,
				[ReadOnly] in SpatialIndex spatialIndex
			)
			{
				DirtyArea dirtyArea = lookupDirtyArea[chunk];
				if (!dirtyArea.active)
					return;
				dirtyArea.active = false;

				atoms = lookupAtoms[chunk];

				Coord from = dirtyArea.From, to = dirtyArea.To;

				int oddity = (tick + from.y) & 0b1;

				// Region size should work ok
				hashKeyAddition = Hash(tick + spatialIndex.origin.y * Space.regionSize + spatialIndex.origin.x);
				int y = from.y;
				int height = to.y * Space.chunkSize;
				for (int rowShift = from.y * Space.chunkSize; rowShift <= height; rowShift += Space.chunkSize)
				{
					if (oddity == 1)
					{
						for (int x = from.x; x <= to.x; x++)
						{
							Entity atom = atoms[rowShift + x];
							if (atom != Entity.Null)
								ProcessAtom(ref dirtyArea, neighbours, atom, x, y);
						}
					}
					else
					{
						for (int x = to.x; x >= from.x; x--)
						{
							Entity atom = atoms[rowShift + x];
							if (atom != Entity.Null)
								ProcessAtom(ref dirtyArea, neighbours, atom, x, y);
						}
					}

					y++;
					oddity ^= 1;
				}

				lookupDirtyArea[chunk] = dirtyArea;
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			private void ProcessAtom(
				ref DirtyArea dirtyArea,
				Neighbourhood neighbours,
				Entity atom,
				int x, int y
			)
			{
				Entity matter = lookupMatter[atom].value;
				Matter.State state = lookupState[matter].value;
				Matter.PhysicProperties physProps = lookupProps[matter];

				switch (state)
				{
					case Matter.State.Solid:
						break;

					case Matter.State.Liquid:
						ProcessLiquidNew(ref dirtyArea, atoms, neighbours, matter, physProps, new Coord(x, y));
						break;

					case Matter.State.Gaseous:
						break;

					default:
						break;
				}
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			private void ProcessLiquidNew(
				ref DirtyArea dirtyArea,
				DynamicBuffer<AtomBufferElement> atoms,
				Neighbourhood neighbours,
				Entity atomMatter,
				Matter.PhysicProperties atomProps,
				Coord atomCoord
			)
            {
                int atomIndex = (atomCoord.y * Space.chunkSize) + atomCoord.x;

                Coord southCoord = atomCoord + Coord.south;
                if (atoms.GetAtomNeighbourFallback(
                    lookupAtoms, neighbours, southCoord,
                    out Entity bottomAtom, out DynamicBuffer<AtomBufferElement> otherAtoms, out Coord otherCoord
                    ) && IsPassable(bottomAtom, atomMatter, atomProps)
                )
                {
                    atoms.Swap(atomIndex, otherAtoms, otherCoord);

                    CoordRect dirtyRect = new(southCoord + Coord.southWest, atomCoord + Coord.northEast);
                    neighbours.MarkDirty(ref dirtyArea, lookupDirtyArea, dirtyRect, safe: true);

                    return;
                }

                int xDir = (((atomIndex + tick) & 0b1) << 1) - 1;
                Coord mainSide = atomCoord, altSide = atomCoord;

                mainSide.x += xDir;
                bool hasMainSide = atoms.GetAtomNeighbourFallback(
                    lookupAtoms, neighbours, mainSide,
                    out Entity mainSideAtom, out DynamicBuffer<AtomBufferElement> mainSwapAtoms, out Coord mainSwapCoord
                );
                bool mainSidePassable = hasMainSide && IsPassable(mainSideAtom, atomMatter, atomProps);

                if (mainSidePassable)
                {
                    Coord mainBottom = mainSide + Coord.south;
                    if (
                        atoms.GetAtomNeighbourFallback(
                        lookupAtoms, neighbours, mainBottom,
                        out bottomAtom, out otherAtoms, out otherCoord
                    ))
                    {
                        if (IsPassable(bottomAtom, atomMatter, atomProps))
                        {
                            AtomBufferExtention.Swap(atoms, atomCoord, otherAtoms, otherCoord);
                            neighbours.MarkDirty(ref dirtyArea, lookupDirtyArea, CoordRect.CreateRectBetween(atomCoord, mainBottom, margin: 1), safe: true);

                            return;
                        }
                    }
                }

                altSide.x -= xDir;
                bool hasAltSide = atoms.GetAtomNeighbourFallback(
                    lookupAtoms, neighbours, altSide,
                    out Entity altSideAtom, out DynamicBuffer<AtomBufferElement> altSwapAtoms, out Coord altSwapCoord
                );
                bool altSidePassable = hasAltSide && IsPassable(altSideAtom, atomMatter, atomProps);

                if (altSidePassable)
                {
                    Coord altBottom = altSide + Coord.south;
                    if (atoms.GetAtomNeighbourFallback(
                        lookupAtoms, neighbours, altBottom,
                        out bottomAtom, out otherAtoms, out otherCoord
                    ))
                    {
                        if (IsPassable(bottomAtom, atomMatter, atomProps))
                        {
                            AtomBufferExtention.Swap(atoms, atomCoord, otherAtoms, otherCoord);
                            neighbours.MarkDirty(ref dirtyArea, lookupDirtyArea, CoordRect.CreateRectBetween(atomCoord, altBottom, margin: 1), safe: true);

                            return;
                        }
                    }
                }

                if (mainSidePassable)
                {
                    AtomBufferExtention.Swap(atoms, atomCoord, mainSwapAtoms, mainSwapCoord);
                    neighbours.MarkDirty(ref dirtyArea, lookupDirtyArea, CoordRect.CreateRectBetween(atomCoord, mainSide, margin: 1), safe: true);

                    return;
                }

                if (altSidePassable)
                {
                    AtomBufferExtention.Swap(atoms, atomCoord, altSwapAtoms, altSwapCoord);
                    neighbours.MarkDirty(ref dirtyArea, lookupDirtyArea, CoordRect.CreateRectBetween(atomCoord, altSide, margin: 1), safe: true);

                    return;
                }
            }

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			private void ProcessLiquidOld(
				ref DirtyArea dirtyArea,
				DynamicBuffer<AtomBufferElement> atoms,
				Neighbourhood neighbours,
				Entity atomMatter,
				Matter.PhysicProperties atomProps,
				Coord atomCoord
			)
			{
				int atomIndex = (atomCoord.y * Space.chunkSize) + atomCoord.x;

                Coord southCoord = atomCoord + Coord.south;
				if (atoms.GetAtomNeighbourFallback(
					lookupAtoms, neighbours, southCoord,
					out Entity bottomAtom, out DynamicBuffer<AtomBufferElement> otherAtoms, out Coord otherCoord
					) && IsPassable(bottomAtom, atomMatter, atomProps)
				)
				{
                    atoms.Swap(atomIndex, otherAtoms, otherCoord);

					CoordRect dirtyRect = new(southCoord + Coord.southWest, atomCoord + Coord.northEast);
					neighbours.MarkDirty(ref dirtyArea, lookupDirtyArea, dirtyRect, safe: true);

					return;
				}

				int xDir = (((atomIndex + tick) & 0b1) << 1)  -1;
				Coord mainSide = atomCoord, altSide = atomCoord;

				mainSide.x += xDir;
				bool hasMainSide = atoms.GetAtomNeighbourFallback(
					lookupAtoms, neighbours, mainSide,
					out Entity mainSideAtom, out DynamicBuffer<AtomBufferElement> mainSwapAtoms, out Coord mainSwapCoord
				);
				bool mainSidePassable = hasMainSide && IsPassable(mainSideAtom, atomMatter, atomProps);

				if (mainSidePassable)
				{
					Coord mainBottom = mainSide + Coord.south;
					if (
						atoms.GetAtomNeighbourFallback(
						lookupAtoms, neighbours, mainBottom,
						out bottomAtom, out otherAtoms, out otherCoord
					))
					{
						if (IsPassable(bottomAtom, atomMatter, atomProps))
						{
							AtomBufferExtention.Swap(atoms, atomCoord, otherAtoms, otherCoord);
							neighbours.MarkDirty(ref dirtyArea, lookupDirtyArea, CoordRect.CreateRectBetween(atomCoord, mainBottom, margin: 1), safe: true);

							return;
						}
					}
				}

				altSide.x -= xDir;
				bool hasAltSide = atoms.GetAtomNeighbourFallback(
					lookupAtoms, neighbours, altSide,
					out Entity altSideAtom, out DynamicBuffer<AtomBufferElement> altSwapAtoms, out Coord altSwapCoord
				);
				bool altSidePassable = hasAltSide && IsPassable(altSideAtom, atomMatter, atomProps);

				if (altSidePassable)
				{
					Coord altBottom = altSide + Coord.south;
					if (atoms.GetAtomNeighbourFallback(
						lookupAtoms, neighbours, altBottom,
						out bottomAtom, out otherAtoms, out otherCoord
					))
					{
						if (IsPassable(bottomAtom, atomMatter, atomProps))
						{
							AtomBufferExtention.Swap(atoms, atomCoord, otherAtoms, otherCoord);
							neighbours.MarkDirty(ref dirtyArea, lookupDirtyArea, CoordRect.CreateRectBetween(atomCoord, altBottom, margin: 1), safe: true);

							return;
						}
					}
				}

				if (mainSidePassable)
				{
					AtomBufferExtention.Swap(atoms, atomCoord, mainSwapAtoms, mainSwapCoord);
					neighbours.MarkDirty(ref dirtyArea, lookupDirtyArea, CoordRect.CreateRectBetween(atomCoord, mainSide, margin: 1), safe: true);
				 
					return;
				}

				if (altSidePassable)
				{
					AtomBufferExtention.Swap(atoms, atomCoord, altSwapAtoms, altSwapCoord);
					neighbours.MarkDirty(ref dirtyArea, lookupDirtyArea, CoordRect.CreateRectBetween(atomCoord, altSide, margin: 1), safe: true);

					return;
				}
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			private bool IsPassable(Entity otherAtom, Entity thisMatter, Matter.PhysicProperties thisProps)
			{
				if (otherAtom == Entity.Null)
					return true;

				Entity otherMatter = lookupMatter[otherAtom].value;
				if (thisMatter == otherMatter)
					return false;
				if (lookupState[otherMatter].value == Matter.State.Solid)
					return false;

				Matter.PhysicProperties otherProps = lookupProps[otherMatter];
				return thisProps.density > otherProps.density;
			}
		}
	}
}