using System;
using System.Runtime.CompilerServices;

using UnityEngine;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Burst;
using Unity.Mathematics;

using static Verse.AtomPhysics;
using static Verse.Chunk;
using static Verse.Atom;

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
            var velocities = GetComponentLookup<Dynamics>();
            var atomBuffers = GetBufferLookup<AtomBufferElement>();

            var dirtyAreas = GetComponentLookup<DirtyArea>();

            JobHandle jobHandle = default;
			for (int i = 0; i < processingBatches; i++)
            {
				physicsQuery.SetSharedComponentFilter(new ProcessingBatchIndex { batchIndex = i });

				jobHandle = new ProcessChunkAtomsJob
				{
					tick = tick,
					lookupMatter = matters,
					lookupState = states,
					lookupProps = physProps,
					lookupAtoms = atomBuffers,
					lookupDirtyArea = dirtyAreas,
					//dynamicsOf = velocities
				}.ScheduleParallel(physicsQuery, JobHandle.CombineDependencies(jobHandle, Dependency));
			}
		}
	}
}