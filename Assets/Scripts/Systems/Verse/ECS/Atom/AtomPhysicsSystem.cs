using Unity.Entities;
using UnityEngine;

using Unity.Jobs;
using Unity.Collections;
using Unity.Burst;
namespace Verse
{
	[UpdateInGroup(typeof(WorldTickSystemGroup))]
	[UpdateAfter(typeof(RegionTextureProcessingSystem))]
	public partial class AtomPhysicsSystem : SystemBase
	{
		private EntityQuery chunkQuery;
		private Entity space;

		private readonly int processingBatches = 4;
		protected override void OnCreate()
		{
			base.OnCreate();

			chunkQuery = GetEntityQuery(
				typeof(ChunkData.DirtyArea),
				ComponentType.ReadOnly<ChunkData.ProcessingBatchIndex>()
			);
			chunkQuery.AddSharedComponentFilter(new ChunkData.ProcessingBatchIndex(-1));
		}

		protected override void OnStartRunning()
		{
			base.OnStartRunning();

			space = GetSingletonEntity<SpaceData.Initialization>();
		}

		protected override void OnUpdate()
		{
			int tick = TickerSystem.CurrentTick;

			for (int i = 0; i < processingBatches; i++)
			{
				chunkQuery.SetSharedComponentFilter(new ChunkData.ProcessingBatchIndex { batchIndex = i });

				new ProcessChunkJob
				{
					tick = tick,
					space = space
				}.ScheduleParallel(chunkQuery).Complete();
			}
		}

		public partial struct ProcessChunkJob : IJobEntity
		{
			[ReadOnly]
			public int tick;

			[ReadOnly]
			public Entity space;

			private Entity chunk;

			public void Execute(
				Entity chunk,
				ref ChunkData.DirtyArea dirtyArea,
				in ChunkData.Neighbours neighbours,
				ref DynamicBuffer<ChunkData.AtomBufferElement> atoms
			)
			{
				if (!dirtyArea.active)
					return;
				dirtyArea.active = false;

				this.chunk = chunk;

				EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

				foreach (Vector2Int coord in dirtyArea.GetSnake(tick))
				{
					Entity atom = atoms.GetAtom(coord);
					if (atom == Entity.Null)
						continue;

					ProcessAtom(entityManager, atoms, neighbours, atom, coord);
				}
			}

			private void ProcessAtom(
				EntityManager entityManager,
				DynamicBuffer<ChunkData.AtomBufferElement> atoms,
				ChunkData.Neighbours neighbours, 
				Entity atom,
				Vector2Int atomCoord
			)
			{
				Entity atomMatter = Atom.GetMatter(entityManager, atom);
				MatterState state = Matter.GetState(entityManager, atomMatter);
				MatterData.PhysicProperties physProps = Matter.GetPhysicalProperties(entityManager, atomMatter);

				switch (state)
				{
					case MatterState.Solid:
						break;

					case MatterState.Liquid:
						ProcessLiquid(entityManager, atoms, neighbours, atom, atomMatter, physProps, atomCoord);
						break;

					case MatterState.Gaseous:
						break;

					default:
						break;
				}
			}

			private void ProcessLiquid(
				EntityManager dstManager,
				DynamicBuffer<ChunkData.AtomBufferElement> atoms,
				ChunkData.Neighbours neighbours,
				Entity atom,
				Entity atomMatter,
				MatterData.PhysicProperties atomProps,
				Vector2Int atomCoord
			)
			{
				foreach (Vector2Int pendulumCoord in Enumerators.GetHalfPendulum(atomCoord, tick))
				{
					if (!atoms.GetAtomNeighbourFallback(
							dstManager,
							neighbours,
							pendulumCoord,
							out Entity otherAtom,
							out DynamicBuffer<ChunkData.AtomBufferElement> otherAtoms,
							out Vector2Int otherCoord
						))
						continue;

					if (otherAtom != Entity.Null)
					{
						Entity otherMatter = Atom.GetMatter(dstManager, otherAtom);
						if (atomMatter == otherMatter)
							continue;

						if (Matter.GetState(dstManager, otherMatter) == MatterState.Solid)
							continue;

						MatterData.PhysicProperties otherProps = Matter.GetPhysicalProperties(dstManager, otherMatter);
						if (atomProps.density <= otherProps.density)
							continue;
					}

					AtomBufferExtention.Swap(atoms, atomCoord, otherAtoms, otherCoord);

					RectInt dirtyRect = RectExtension.CreateRectBetween(atomCoord, otherCoord, margin: 1, additiveSize: 1);
					Space.MarkDirty(dstManager, space, dirtyRect, safe: true);

					break;
				}
			}
		}
	}
}