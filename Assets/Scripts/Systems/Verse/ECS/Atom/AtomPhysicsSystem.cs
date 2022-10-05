using Unity.Entities;
using UnityEngine;

using Unity.Jobs;
using Unity.Collections;
using Unity.Burst;
using System.Linq;

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
				//ComponentType.ReadWrite<Chunk.DirtyArea>(),
				ComponentType.ReadOnly<Chunk.ProcessingBatchIndex>()
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
				}.Schedule(chunkQuery, Dependency).Complete();
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
			public BufferLookup<Chunk.AtomBufferElement> atomBuffers;
			public ComponentLookup<Chunk.DirtyArea> dirtyAreas;

			public void Execute(
				Entity chunk,
				in Chunk.Neighbourhood neighbours
			)
			{
				Chunk.DirtyArea dirtyArea = dirtyAreas[chunk];

				if (!dirtyArea.active)
					return;
				dirtyArea.active = false;

				DynamicBuffer<Chunk.AtomBufferElement> atoms = atomBuffers[chunk];

				Chunk.DirtyArea copiedArea = dirtyArea;
				foreach (Vector2Int coord in copiedArea.GetSnake(tick))
				{
					Entity atom = atoms.GetAtom(coord);
					if (atom == Entity.Null)
						continue;

					if (atom.Index < 0)
						continue;

					ProcessAtom(ref dirtyArea, atoms, neighbours, atom, coord);
				}

				dirtyAreas[chunk] = dirtyArea;
			}

			private void ProcessAtom(
				ref Chunk.DirtyArea dirtyArea,
				DynamicBuffer<Chunk.AtomBufferElement> atoms,
				Chunk.Neighbourhood neighbours,
				Entity atom,
				Vector2Int atomCoord
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
						ProcessLiquid(ref dirtyArea, atoms, neighbours, atom, matter, physProps, atomCoord);
						break;

					case Matter.State.Gaseous:
						break;

					default:
						break;
				}
			}

			private void ProcessLiquid(
				ref Chunk.DirtyArea dirtyArea,
				DynamicBuffer<Chunk.AtomBufferElement> atoms,
				Chunk.Neighbourhood neighbours,
				Entity atom,
				Entity atomMatter,
				Matter.PhysicProperties atomProps,
				Vector2Int atomCoord
			)
			{
				foreach (Vector2Int pendulumCoord in Enumerators.GetHalfPendulum(atomCoord, tick))
				{
					if (!atoms.GetAtomNeighbourFallback(
							atomBuffers,
							neighbours,
							pendulumCoord,
							out Entity otherAtom,
							out DynamicBuffer<Chunk.AtomBufferElement> otherAtoms,
							out Vector2Int otherCoord
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

					RectInt dirtyRect = RectExtension.CreateRectBetween(atomCoord, pendulumCoord, margin: 1);

					dirtyArea.MarkDirty(dirtyRect, safe: true);
					neighbours.MarkDirty(dirtyAreas, dirtyRect, safe: true);

					break;
				}
			}
		}
	}
}