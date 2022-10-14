using System;
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
				ComponentType.ReadOnly<MeshCollider>(),
				ComponentType.ReadWrite<Chunk.ColliderStatus>()
			);

			outputPoints = new NativeArray<float2>(Space.chunkSize * Space.chunkSize, Allocator.Persistent);
			solidityMap = new NativeArray<bool>(Space.chunkSize * Space.chunkSize, Allocator.Persistent);

			int contouringCellsLength = Space.chunkSize - 1;
			contouringCellsLength *= contouringCellsLength;
			contouringCells = new NativeArray<byte>(contouringCellsLength, Allocator.Persistent);
		}

		protected override void OnStartRunning()
		{
			base.OnStartRunning();

			/// TESTING SECTION
			
			Debug.Log($"Running marching squares test");
			const int mapWidth = 4;
			NativeArray<bool> _solidityMap = new(new bool[mapWidth * mapWidth]
			{
				true,	true,	true,	true,
				true,	false,	false,	true,
				true,	false,	false,	true,
				true,	true,	true,	true
			}, Allocator.Persistent);

			NativeArray<byte> _contouringCells = new((mapWidth - 1) * (mapWidth - 1), Allocator.Persistent);
			for (int y = 0; y < mapWidth - 1; y ++)
				for (int x = 0; x < mapWidth - 1; x++)
				{
					int index = y * mapWidth + x;
					_contouringCells[y * (mapWidth - 1) + x] = GetContouring(
						_solidityMap[index],
						_solidityMap[index + 1],
						_solidityMap[index + mapWidth],
						_solidityMap[index + mapWidth + 1]
					);
				}

			string _contouringString = "";
			for (int i = mapWidth - 2; i >= 0; i--)
			{
				for (int j = 0; j < mapWidth - 1; j++)
					_contouringString += Convert.ToString(_contouringCells[i * (mapWidth - 1) + j], 2).PadLeft(4, '0') + " ";
				_contouringString += '\n';
			}
			Debug.Log(_contouringString);

			NativeArray<float2> _points = new NativeArray<float2>(_contouringCells.Length, Allocator.Persistent);
			Contour[] _contours = new Contour[]
			{
				new Contour(),
				new Contour(new float2(.5f, 0f), new float2(1f, .5f)),
				new Contour(new float2(0f, .5f), new float2(.5f, 0f)),
				new Contour(new float2(0f, .5f), new float2(1f, .5f)),
				new Contour(new float2(.5f, 1f), new float2(1f, .5f)),
				new Contour(new float2(.5f, 1f), new float2(.5f, 0f)),
				new Contour(new float2(0f, .5f), new float2(.5f, 1f), new float2(.5f, 0f), new float2(1f, .5f)),
                new Contour(new float2(0f, .5f), new float2(1f, .5f)),

                new Contour(new float2(0f, .5f), new float2(.5f, 1f)),
				new Contour(new float2(0f, .5f), new float2(.5f, 0f), new float2(.5f, 1f), new float2(1f, .5f)),
                new Contour(new float2(.5f, 1f), new float2(1f, .5f)),
                new Contour(new float2(0f, .5f), new float2(1f, .5f)),
                new Contour(new float2(0f, .5f), new float2(.5f, 0f)),
                new Contour(new float2(.5f, 0f), new float2(1f, .5f)),
				new Contour()
            };



            /// END OF TESTING SECTION
        }

		private struct Contour
		{
			public float2[] points;

			public Contour(float2 p1) { points = new[] { p1 }; }
			public Contour(float2 p1, float2 p2) { points = new[] { p1, p2 }; }
			public Contour(float2 p1, float2 p2, float2 p3, float2 p4) { points = new[] { p1, p2, p3, p4 }; }
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

					bool solid = state == colliderState;
					solidityMap[i] = solid;
				}

				int rowShift = 0;
				for (int y = 0; y < Space.chunkSize - 1; y++)
				{
					for (int x = 0; x < Space.chunkSize - 1; x++)
					{
						int index = rowShift + x;
						contouringCells[y * (Space.chunkSize - 1) + x] = GetContouring(
							solidityMap[index],
							solidityMap[index + 1],
							solidityMap[index + Space.chunkSize],
							solidityMap[index + Space.chunkSize + 1]
						);
					}
					rowShift += Space.chunkSize;
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
				in MeshCollider collider,
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