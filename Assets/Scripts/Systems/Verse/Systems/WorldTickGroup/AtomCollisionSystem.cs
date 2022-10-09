using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;

namespace Verse
{
	[UpdateInGroup(typeof(WorldTickSystemGroup))]
	[UpdateAfter(typeof(AtomPhysicsSystem))]
	public partial class AtomCollisionSystem : SystemBase
	{
		private EntityQuery chunkQuery;

		protected override void OnCreate()
		{
			base.OnCreate();

			chunkQuery = GetEntityQuery(
				ComponentType.ReadWrite<Chunk.DirtyArea>()
			);
		}

		protected override void OnUpdate()
		{
			var matters = GetComponentLookup<Atom.Matter>(isReadOnly: true);
			var states = GetComponentLookup<Matter.AtomState>(isReadOnly: true);
			var atomBuffers = GetBufferLookup<Chunk.AtomBufferElement>(isReadOnly: true);

			new GenerateChunkColliderPointsJob
			{
				matters = matters,
				states = states,
				atomBuffers = atomBuffers
			}.ScheduleParallel(chunkQuery, Dependency);
		}

		// [BurstCompile]
		public partial struct GenerateChunkColliderPointsJob : IJobEntity
		{
			[ReadOnly]
			public ComponentLookup<Atom.Matter> matters;
			[ReadOnly]
			public ComponentLookup<Matter.AtomState> states;
			[ReadOnly]
			public BufferLookup<Chunk.AtomBufferElement> atomBuffers;

			public void Execute(
				Entity chunk
			)
			{
				DynamicBuffer<Chunk.AtomBufferElement> atoms = atomBuffers[chunk];

				int maxRowShift = Space.chunkSize * Space.chunkSize;
				for (int rowShift = 0 * Space.chunkSize; rowShift < maxRowShift; rowShift += Space.chunkSize)
				{
					for (int x = 0; x < Space.chunkSize; x++)
					{

					}
				}
			}
		}

		public partial struct RebuildChunkColliderJob : IJobEntity
		{
			[ReadOnly]
			NativeArray<Coord> coords;

			public void Execute(
				Entity chunk,
				in PolygonCollider2D collider
			)
			{
				Vector2[] points = coords.Cast<Vector2>().ToArray();
				collider.SetPath(0, points);
			}
		}
	}
}