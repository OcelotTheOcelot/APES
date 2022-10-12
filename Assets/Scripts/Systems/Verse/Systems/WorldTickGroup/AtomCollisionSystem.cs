using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;

namespace Verse
{
	[UpdateInGroup(typeof(WorldTickSystemGroup))]
	[UpdateAfter(typeof(AtomPhysicsSystem))]
	public partial class AtomCollisionSystem : SystemBase
	{
		private EntityQuery calculationQuery;
		private EntityQuery rebuildingQuery;

		private NativeArray<bool> solidityMap;
		private NativeArray<byte> contouringCells;

		private NativeArray<float2> outputPoints;

		protected override void OnCreate()
		{
			base.OnCreate();

			calculationQuery = GetEntityQuery(
				ComponentType.ReadWrite<Chunk.DirtyArea>(),
				ComponentType.ReadOnly<Chunk.ColliderStatus>()
			);

			rebuildingQuery = GetEntityQuery(
				ComponentType.ReadOnly<PolygonCollider2D>(),
				ComponentType.ReadWrite<Chunk.ColliderStatus>()
			);

			outputPoints = new NativeArray<float2>(Space.chunkSize * Space.chunkSize, Allocator.Persistent);
			solidityMap = new NativeArray<bool>(Space.chunkSize * Space.chunkSize, Allocator.Persistent);

			int contouringCellsLength = Space.chunkSize - 1;
			contouringCellsLength *= contouringCellsLength;
			contouringCells = new NativeArray<byte>(contouringCellsLength, Allocator.Persistent);
		}

		protected override void OnUpdate()
		{
			var matters = GetComponentLookup<Atom.Matter>(isReadOnly: true);
			var states = GetComponentLookup<Matter.AtomState>(isReadOnly: true);

			EntityCommandBuffer commandBuffer = GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(World.Unmanaged);

			var calculationJobHandle = new CalculateChunkColliderPointsJob
			{
				ecb = commandBuffer,
				matters = matters,
				states = states,
				outputPoints = outputPoints,
				solidityMap = solidityMap,
				contouringCells = contouringCells
			}.Schedule(calculationQuery, Dependency);
			calculationJobHandle.Complete();

			new RebuildChunkColliderJob
			{
				points = outputPoints
			}.Run(rebuildingQuery);
		}

		[BurstCompile]
		public partial struct CalculateChunkColliderPointsJob : IJobEntity
		{
			[ReadOnly]
			public ComponentLookup<Atom.Matter> matters;
			[ReadOnly]
			public ComponentLookup<Matter.AtomState> states;

			[ReadOnly]
			public Matter.State colliderState;

			[WriteOnly]
			public NativeArray<float2> outputPoints;

			public NativeArray<bool> solidityMap;
			public NativeArray<byte> contouringCells;
			public EntityCommandBuffer ecb;

			public void Execute(
				DynamicBuffer<Chunk.AtomBufferElement> atoms,
				in Chunk.ColliderStatus colliderStatus
			)
			{
				if (!colliderStatus.pendingRebuild)
					return;

				for (int i = 0; i < solidityMap.Length; i++)
				{
					Entity atom = atoms[i];

					if (atom == Entity.Null)
						continue;

					Entity matter = matters[atom].matter;

					Matter.State state = states[matter].state;
					solidityMap[i] = state == colliderState;
				}

				for (int i = 0; i < contouringCells.Length; i++)
				{
					contouringCells[i] = GetContouring(
						solidityMap[i],
						solidityMap[i + 1],
						solidityMap[i + Space.chunkSize],
						solidityMap[i + Space.chunkSize + 1]
					);
				}
			}

			private byte GetContouring(bool sw, bool se, bool nw, bool ne)
			{
				byte contour = 0b0000;

				if (sw)
					contour |= 0b0010;
				if (se)
					contour |= 0b0001;
				if (nw)
					contour |= 0b1000;
				if (ne)
					contour |= 0b0100;

				return contour;
			}
		}

		public partial struct RebuildChunkColliderJob : IJobEntity
		{
			[ReadOnly]
			public NativeArray<float2> points;

			public void Execute(
				in PolygonCollider2D collider,
				[WriteOnly] ref Chunk.ColliderStatus colliderStatus
			)
			{
				if (!colliderStatus.pendingRebuild)
					return;

				Vector2[] pointArray = new Vector2[points.Length];
				for (int i = 0; i < points.Length; i++)
					pointArray[i] = points[i];

				if (collider == null)
					Debug.LogWarning("Warning: null chunk collider!");
				else
				{
					//collider.SetPath(0,
					//	new Vector2[] {
					//		new Vector2(0, 0),
					//		new Vector2(0, Space.chunkSize),
					//		new Vector2(Space.chunkSize, Space.chunkSize),
					//		new Vector2(Space.chunkSize, 0)
					//	}
					//);
					
					// collider.SetPath(0, pointArray);
				}

				colliderStatus.pendingRebuild = false;
			}
		}
	}
}