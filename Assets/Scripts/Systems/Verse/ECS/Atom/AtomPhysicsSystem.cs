using Unity.Entities;
using UnityEngine;

using Unity.Jobs;
using Unity.Collections;
using Unity.Burst;
using System.Linq;
using Unity.Burst.Intrinsics;

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

		protected override void OnStartRunning()
		{
			base.OnStartRunning();
		}

		protected override void OnUpdate()
		{
			int tick = TickerSystem.CurrentTick;

			JobHandle jobHandle = default;

			var matters = GetComponentLookup<Atom.Matter>(isReadOnly: true);
			var states = GetComponentLookup<Matter.AtomState>(isReadOnly: true);
			var physProps = GetComponentLookup<Matter.PhysicProperties>(isReadOnly: true);
			var dirtyAreas = GetComponentLookup<Chunk.DirtyArea>();
			var atomBuffers = GetBufferLookup<Chunk.AtomBufferElement>();

			for (int i = 0; i < processingBatches; i++)
			{
				chunkQuery.SetSharedComponentFilter(new Chunk.ProcessingBatchIndex { batchIndex = i });

				jobHandle = new ProcessChunkJob
				{
					tick = tick,
					matters = matters,
					states = states,
					physicProperties = physProps,
					dirtyAreas = dirtyAreas,
					atomBuffers = atomBuffers
				}.ScheduleParallel(chunkQuery, jobHandle);
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
				Coord coord;

				int oddity = (tick + from.y) & 0b1;
				int width = to.x - from.x;

				for (int y = from.y; y <= to.y; y++)
				{
					for (int x = 0; x <= width; x++)
					{
                        if (oddity == 1)
							coord = new Coord(from.x + x, y);
						else
							coord = new Coord(to.x - x, y);

						Entity atom = atoms.GetAtom(coord);
						if (atom == Entity.Null)
							continue;

						if (atom.Index < 0)
							continue;

						ProcessAtom(ref area, atoms, neighbours, atom, coord);
					}

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