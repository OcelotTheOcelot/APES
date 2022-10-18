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
using UnityEngine.Rendering;

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

		private ContourData outputData;

		protected override void OnCreate()
		{
			base.OnCreate();

			RequireForUpdate<Space.Tag>();

			calculationQuery = GetEntityQuery(
				ComponentType.ReadWrite<Chunk.DirtyArea>(),
				ComponentType.ReadOnly<Chunk.ColliderStatus>()
			);

			rebuildingQuery = GetEntityQuery(
				//ComponentType.ReadOnly<MeshCollider>(),
				ComponentType.ReadWrite<Chunk.ColliderStatus>()
			);

			outputData = new ContourData()
			{
				segments = new NativeArray<Segment>((int)Mathf.Pow(Space.chunkSize - 1, 2) * 4, Allocator.Persistent),
				finalSegmentIndexes = new NativeList<int>((int)Mathf.Pow(Space.chunkSize / 2, 2), Allocator.Persistent)
			};
			solidityMap = new NativeArray<bool>(Space.totalCellsInChunk, Allocator.Persistent);

			int contouringCellsLength = Space.chunkSize - 1;
			contouringCellsLength *= contouringCellsLength;
			contouringCells = new NativeArray<byte>(contouringCellsLength, Allocator.Persistent);
		}

		protected override void OnStartRunning()
		{
			base.OnStartRunning();

			/// TESTING SECTION
			
			const int mapWidth = 6;
			NativeArray<bool> _solidityMap = new(new bool[mapWidth * mapWidth]
			{
				false,  true,	true,	false,  true,   false,
				false,  false,  true,   false,  true,   true,
				false,  true,   false,	false,  true,   false,
				true,   true,   true,   true,   false,  false,
				false,  false,  false,  false,  true,   true,
				true,   false,  true,   true,   false,  false,
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
					_contouringString += Convert.ToString(_contouringCells[i * (mapWidth - 1) + j], 2).PadLeft(4, '0') + "\t";
				_contouringString += '\n';
			}
			Debug.Log(_contouringString);

			ContourData _contourData = new()
			{
				segments = new NativeArray<Segment>((int)Mathf.Pow(mapWidth - 1, 2) * 4, Allocator.Persistent),
				finalSegmentIndexes = new NativeList<int>((int)Mathf.Pow(mapWidth / 2, 2), Allocator.Persistent)
			};

			/// END OF TESTING SECTION
		}

		public struct Segment
		{
			public float2 from;
			public float2 to;

			public void Get3d(out Vector3 from, out Vector3 to)
			{
				from = (Vector2)this.from;
				to = (Vector2)this.to;
			}

			public Segment(float2 from, float2 to) { this.from = from; this.to = to; }
		}

		public struct ContourData
		{
			public NativeArray<Segment> segments;
			public NativeList<int> finalSegmentIndexes;
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
				matters = matters,
				states = states,
				outputData = outputData,
				solidityMap = solidityMap,
				contouringCells = contouringCells
			}.Schedule(calculationQuery, Dependency);
			calculationJobHandle.Complete();

			new RebuildChunkColliderJob
			{
				contourData = outputData
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
			public ContourData outputData;

			public NativeArray<bool> solidityMap;
			public NativeArray<byte> contouringCells;

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

					Entity matter = matters[atom].value;

					Matter.State state = states[matter].value;

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

				outputData.segments[0] = new Segment(new float2(64 * Space.metersPerCell, 64 * Space.metersPerCell), new float2(0, 64 * Space.metersPerCell));
				outputData.segments[1] = new Segment(new float2(0, 64 * Space.metersPerCell), new float2(0, 0));
				outputData.segments[2] = new Segment(new float2(0, 0), new float2(64 * Space.metersPerCell, 0));
				outputData.segments[3] = new Segment(new float2(64 * Space.metersPerCell, 0), new float2(64 * Space.metersPerCell, 64 * Space.metersPerCell));
				outputData.finalSegmentIndexes.Length = 1;
				outputData.finalSegmentIndexes[0] = 3;

				// Ramer–Douglas–Peucker
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
			public ContourData contourData;

			public void Execute(
				[WriteOnly] ref Chunk.ColliderStatus colliderStatus
			)
			{
				if (!colliderStatus.pendingRebuild)
					return;

				//if (collider == null)
				//	Debug.LogWarning("Warning: null chunk collider!");
				//else
				//{
				//	//collider.SetPath(0,
				//	//	new Vector2[] {
				//	//		new Vector2(0, 0),
				//	//		new Vector2(0, Space.chunkSize),
				//	//		new Vector2(Space.chunkSize, Space.chunkSize),
				//	//		new Vector2(Space.chunkSize, 0)
				//	//	}
				//	//);

				//	// collider.SetPath(0, pointArray);

				//	// collider.sharedMesh = PolygonsToMesh(contourData);
				//}

				colliderStatus.pendingRebuild = false;
			}

			private Mesh PolygonsToMesh(ContourData data)
			{
				Mesh mesh = new();

				int segmentCount = data.finalSegmentIndexes[^1] + 1;

				Vector3[] vertices = new Vector3[segmentCount * 4];  // When ordered, should use 2 + N*2;
				int[] triangles = new int[segmentCount * 2 * 3];

				const float depth = .5f;

				int islandIndex = 0;
				int currentFinalIndex = data.finalSegmentIndexes[islandIndex];

				int verticeIndex = 0;
				int triangleIndex = 0;
				for (int i = 0; i < segmentCount; i++)
				{

					Segment segment = data.segments[i];
					segment.Get3d(out Vector3 from, out Vector3 to);

					vertices[verticeIndex] = from;
					vertices[verticeIndex + 1] = to;
					from.z += depth;
					to.z += depth;
					vertices[verticeIndex + 2] = from;
					vertices[verticeIndex + 3] = to;

					triangles[triangleIndex++] = verticeIndex + 2;
					triangles[triangleIndex++] = verticeIndex + 1;
					triangles[triangleIndex++] = verticeIndex;

					triangles[triangleIndex++] = verticeIndex + 2;
					triangles[triangleIndex++] = verticeIndex + 3;
					triangles[triangleIndex++] = verticeIndex + 1;

					verticeIndex += 4;

					if (i == currentFinalIndex && i + 1 < segmentCount)
						currentFinalIndex = data.finalSegmentIndexes[++islandIndex];
				}

				mesh.vertices = vertices;
				mesh.triangles = triangles;

				return mesh;
			}
		}
	}
}