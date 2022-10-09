#define UNITY_BURST_EXPERIMENTAL_LOOP_INTRINSICS

using Unity.Entities;
using UnityEngine;

using Unity.Jobs;
using Unity.Collections;
using Unity.Burst;
using System.Linq;
using Unity.Burst.Intrinsics;
using Unity.Burst.CompilerServices;

namespace Verse
{
	[UpdateInGroup(typeof(WorldTickSystemGroup))]
	public partial class AtomPhysicsSystem : SystemBase
	{
		private EntityQuery chunkQuery;

		private readonly int processingBatches = 4;
		protected override void OnCreate()
		{
			base.OnCreate();

			chunkQuery = GetEntityQuery(
				ComponentType.ReadWrite<Chunk.DirtyArea>(),
				ComponentType.ReadOnly<Chunk.ProcessingBatchIndex>(),
				ComponentType.ReadOnly<Chunk.Neighbourhood>()
			);
			chunkQuery.AddSharedComponentFilter(new Chunk.ProcessingBatchIndex(-1));
		}

		protected override void OnUpdate()
		{
			int tick = TickerSystem.CurrentTick;

			var matters = GetComponentLookup<Atom.Matter>(isReadOnly: true);
			var states = GetComponentLookup<Matter.AtomState>(isReadOnly: true);
			var physProps = GetComponentLookup<Matter.PhysicProperties>(isReadOnly: true);
			var dirtyAreas = GetComponentLookup<Chunk.DirtyArea>();
			var atomBuffers = GetBufferLookup<Chunk.AtomBufferElement>();

			for (int i = 0; i < processingBatches; i++)
			{
				chunkQuery.SetSharedComponentFilter(new Chunk.ProcessingBatchIndex { batchIndex = i });

				new ProcessChunkJob
				{
					tick = tick,
					matters = matters,
					states = states,
					physicProperties = physProps,
					dirtyAreas = dirtyAreas,
					atomBuffers = atomBuffers
				}.ScheduleParallel(chunkQuery, Dependency).Complete();
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
			public ComponentLookup<Chunk.DirtyArea> dirtyAreas;
			[NativeDisableParallelForRestriction]
			public BufferLookup<Chunk.AtomBufferElement> atomBuffers;

			public void Execute(
				Entity chunk,
				in Chunk.Neighbourhood neighbours
			)
			{
				Chunk.DirtyArea area = dirtyAreas[chunk];

				if (!area.active)
					return;
				area.active = false;

				DynamicBuffer<Chunk.AtomBufferElement> atoms = atomBuffers[chunk];

				Coord from = area.from, to = area.to;

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
							if (atom == Entity.Null)
								continue;

							Coord coord = new(x, y);
							ProcessAtom(ref area, atoms, neighbours, atom, coord);
						}
					}
					else
					{
						for (int x = to.x; x >= from.x; x--)
						{
							Entity atom = atoms[rowShift + x];
							if (atom == Entity.Null)
								continue;

							Coord coord = new(x, y);
							ProcessAtom(ref area, atoms, neighbours, atom, coord);
						}
					}

					y++;
					oddity ^= 1;
				}

				dirtyAreas[chunk] = area;
			}

			private void ProcessAtom(
				ref Chunk.DirtyArea dirtyArea,
				DynamicBuffer<Chunk.AtomBufferElement> atoms,
				Chunk.Neighbourhood neighbours,
				Entity atom,
				Coord atomCoord
			)
			{
				Entity matter = matters[atom].matter;
				Matter.State state = states[matter].state;
				Matter.PhysicProperties physProps = physicProperties[matter];

				switch (state)
				{
					case Matter.State.Solid:
						break;

					case Matter.State.Liquid:
						ProcessLiquid(ref dirtyArea, atoms, neighbours, matter, physProps, atomCoord);
						break;

					case Matter.State.Gaseous:
						break;

					default:
						break;
				}
			}

			private void ProcessSolid()
			{
			}

			private void ProcessLiquid(
				ref Chunk.DirtyArea dirtyArea,
				DynamicBuffer<Chunk.AtomBufferElement> atoms,
				Chunk.Neighbourhood neighbours,
				Entity atomMatter,
				Matter.PhysicProperties atomProps,
				Coord atomCoord
			)
			{
				Coord[] halfPendulum = (tick & 0b1) == 1 ? Enumerators.halfPendulumRight : Enumerators.halfPendulumLeft;

				foreach (Coord pendulumShift in halfPendulum)
				{
					Coord pendulumCoord = atomCoord + pendulumShift;

					if (!atoms.GetAtomNeighbourFallback(
							atomBuffers,
							neighbours,
							pendulumCoord,
							out Entity otherAtom,
							out DynamicBuffer<Chunk.AtomBufferElement> otherAtoms,
							out Coord otherCoord
						))
						continue;

					if (otherAtom != Entity.Null)
					{
						Entity otherMatter = matters[otherAtom].matter;
						if (atomMatter == otherMatter)
							continue;

						if (states[otherMatter].state == Matter.State.Solid)
							continue;

						Matter.PhysicProperties otherProps = physicProperties[otherMatter];
						if (atomProps.density <= otherProps.density)
							continue;
					}

					AtomBufferExtention.Swap(atoms, atomCoord, otherAtoms, otherCoord);

					CoordRect dirtyRect = CoordRect.CreateRectBetween(atomCoord, pendulumCoord, margin: 1);

					dirtyArea.MarkDirty(dirtyRect, safe: true);
					neighbours.MarkDirty(dirtyAreas, dirtyRect, safe: true);

					break;
				}
			}
		}
	}
}