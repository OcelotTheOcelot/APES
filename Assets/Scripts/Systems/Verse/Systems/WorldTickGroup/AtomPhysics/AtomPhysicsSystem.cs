using Unity.Entities;

using Unity.Jobs;
using Unity.Collections;
using Unity.Burst;

using Unity.Mathematics;
using System.Runtime.CompilerServices;
using UnityEngine;
using static Verse.Chunk;
using static Verse.ParticlePhysics;
using System;
using Apes.UI;
using Apes.Random;

namespace Verse
{
	[UpdateInGroup(typeof(VerseTickSystemGroup))]
	public partial class AtomPhysicsSystem : SystemBase
	{
		private EntityQuery physicsQuery;

		private readonly int processingBatches = 4;
		protected override void OnCreate()
		{
			base.OnCreate();

			physicsQuery = GetEntityQuery(
				ComponentType.ReadOnly<DirtyArea>(),
				ComponentType.ReadOnly<ProcessingBatchIndex>(),
				ComponentType.ReadOnly<Neighbourhood>()
			);
			physicsQuery.AddSharedComponentFilter(new ProcessingBatchIndex(-1));
		}

		protected override void OnUpdate()
		{
			int tick = TickerSystem.CurrentTick;

			var matters = GetComponentLookup<Atom.Matter>(isReadOnly: true);
			var states = GetComponentLookup<Matter.AtomState>(isReadOnly: true);
			var physProps = GetComponentLookup<Matter.PhysicProperties>(isReadOnly: true);
			var atomBuffers = GetBufferLookup<AtomBufferElement>();

			var dirtyAreas = GetComponentLookup<DirtyArea>();

			JobHandle jobHandle = default;
			for (int i = 0; i < processingBatches; i++)
			{
				physicsQuery.SetSharedComponentFilter(new Chunk.ProcessingBatchIndex { batchIndex = i });

				jobHandle = new ProcessChunkJob
				{
					tick = tick,
					matters = matters,
					states = states,
					physicProperties = physProps,
					atomBuffers = atomBuffers,
					dirtyAreas = dirtyAreas
				}.ScheduleParallel(physicsQuery, jobHandle);
				jobHandle.Complete();
			}
		}

		[BurstCompile]
		public partial struct ProcessChunkJob : IJobEntity
		{
			[ReadOnly]
			public int tick;
			[ReadOnly]
			public ComponentLookup<Atom.Matter> matters;
			[ReadOnly]
			public ComponentLookup<Matter.AtomState> states;
			[ReadOnly]
			public ComponentLookup<Matter.PhysicProperties> physicProperties;

			[NativeDisableParallelForRestriction]
			public BufferLookup<AtomBufferElement> atomBuffers;
			[NativeDisableParallelForRestriction]
			public ComponentLookup<DirtyArea> dirtyAreas;

			[NativeDisableParallelForRestriction]
			private DynamicBuffer<AtomBufferElement> atoms;

			public void Execute(
				Entity chunk,
				in Neighbourhood neighbours
			)
			{
				DirtyArea dirtyArea = dirtyAreas[chunk];
				if (!dirtyArea.active)
					return;
				dirtyArea.active = false;

				atoms = atomBuffers[chunk];

				Coord from = dirtyArea.From, to = dirtyArea.To;

				int oddity = (tick + from.y) & 0b1;

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

				dirtyAreas[chunk] = dirtyArea;
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			private void ProcessAtom(
				ref DirtyArea dirtyArea,
				Neighbourhood neighbours,
				Entity atom,
				int x, int y
			)
			{
				Entity matter = matters[atom].value;
				Matter.State state = states[matter].value;
				Matter.PhysicProperties physProps = physicProperties[matter];

				switch (state)
				{
					case Matter.State.Solid:
						break;

					case Matter.State.Liquid:
						ProcessLiquid(ref dirtyArea, atoms, neighbours, matter, physProps, new Coord(x, y));
						break;

					case Matter.State.Gaseous:
						break;

					default:
						break;
				}
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			private void ProcessLiquid(
				ref DirtyArea dirtyArea,
				DynamicBuffer<AtomBufferElement> atoms,
				Neighbourhood neighbours,
				Entity atomMatter,
				Matter.PhysicProperties atomProps,
				Coord atomCoord
			)
			{
				Coord southCoord = atomCoord + Coord.south;
				if (atoms.GetAtomNeighbourFallback(
					atomBuffers, neighbours, southCoord,
					out Entity bottomAtom, out DynamicBuffer<AtomBufferElement> otherAtoms, out Coord otherCoord
					) && IsPassable(bottomAtom, atomMatter, atomProps))
				{
					AtomBufferExtention.Swap(atoms, atomCoord, otherAtoms, otherCoord);

					CoordRect dirtyRect = new(southCoord + Coord.southWest, atomCoord + Coord.northEast);
					neighbours.MarkDirty(ref dirtyArea, dirtyAreas, dirtyRect, safe: true);

					return;
				}

				int xDir = ((int)(atomCoord.x * math.PI) & atomCoord.y & tick & 0b1) == 1 ? 1 : -1;
				Coord mainSide = atomCoord, altSide = atomCoord;

				mainSide.x += xDir;
				bool hasMainSide = atoms.GetAtomNeighbourFallback(
					atomBuffers, neighbours, mainSide,
					out Entity mainSideAtom, out DynamicBuffer<AtomBufferElement> mainSideAtoms, out Coord mainSideCoord
				);
				bool mainSidePassable = hasMainSide && IsPassable(mainSideAtom, atomMatter, atomProps);

				if (mainSidePassable && atoms.GetAtomNeighbourFallback(
					atomBuffers, neighbours, mainSide,
					out bottomAtom, out otherAtoms, out otherCoord
				))
				{
					if (IsPassable(bottomAtom, atomMatter, atomProps))
					{
						AtomBufferExtention.Swap(atoms, atomCoord, otherAtoms, otherCoord);
						neighbours.MarkDirty(ref dirtyArea, dirtyAreas, CoordRect.CreateRectBetween(atomCoord, otherCoord, margin: 1), safe: true);

						return;
					}
				}

				altSide.x -= xDir;
				bool hasAltSide = atoms.GetAtomNeighbourFallback(
					atomBuffers, neighbours, altSide,
					out Entity altSideAtom, out DynamicBuffer<AtomBufferElement> altSideAtoms, out Coord altSideCoord
				);
				bool altSidePassable = hasAltSide && IsPassable(altSideAtom, atomMatter, atomProps);

				if (altSidePassable && atoms.GetAtomNeighbourFallback(
					atomBuffers, neighbours, altSide,
					out bottomAtom, out otherAtoms, out otherCoord
				))
				{
					if (IsPassable(bottomAtom, atomMatter, atomProps))
					{
						AtomBufferExtention.Swap(atoms, atomCoord, otherAtoms, otherCoord);
						neighbours.MarkDirty(ref dirtyArea, dirtyAreas, CoordRect.CreateRectBetween(atomCoord, otherCoord, margin: 1), safe: true);

						return;
					}
				}

				if (altSidePassable)
				{
					AtomBufferExtention.Swap(atoms, atomCoord, altSideAtoms, altSide);
					neighbours.MarkDirty(ref dirtyArea, dirtyAreas, CoordRect.CreateRectBetween(atomCoord, altSide, margin: 1), safe: true);
					
					return;
				}
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			private bool IsPassable(Entity otherAtom, Entity thisMatter, Matter.PhysicProperties thisProps)
			{
				if (otherAtom == Entity.Null)
					return true;

				Entity otherMatter = matters[otherAtom].value;
				if (thisMatter == otherMatter)
					return false;
				if (states[otherMatter].value == Matter.State.Solid)
					return false;

				Matter.PhysicProperties otherProps = physicProperties[otherMatter];
				return thisProps.density <= otherProps.density;
			}
		}
	}
}