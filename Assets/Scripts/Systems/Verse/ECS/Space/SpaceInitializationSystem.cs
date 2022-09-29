using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Verse
{
	[UpdateInGroup(typeof(WorldInitializationSystemGroup), OrderFirst = true)]
	public class SpaceInitializationSystem : ComponentSystem
	{
		public static Entity SpaceEntity { get; private set; }

		public static Entity RegionPrefab { get; private set; }
		public static Entity ChunkPrefab { get; private set; }
		public static Entity AtomPrefab { get; private set; }

		private float metersPerCell;

		protected override void OnStartRunning()
		{
			base.OnStartRunning();

			SpaceEntity = GetSingletonEntity<SpaceData.Size>();
			SpaceData.Size sizeData = GetSingleton<SpaceData.Size>();

			metersPerCell = 1f / sizeData.cellsPerMeter;

			RegionPrefab = GetSingleton<RegionPrefabData>().prefab;
			ChunkPrefab = GetSingleton<ChunkPrefabData>().prefab;
			AtomPrefab = GetSingleton<AtomPrefabData>().prefab;

			Vector2Int regionCount = GetSingleton<SpaceData.Initialization>().regionCount;
			foreach (Vector2Int chunkPos in Enumerators.GetRect(regionCount))
				InstantiateRegion(chunkPos);

			Debug.Log($"Space of {regionCount.x} by {regionCount.y} chunks has been created.");
		}

		protected override void OnUpdate() {}

		private void InstantiateRegion(Vector2Int chunkPos)
		{
			Entity region = EntityManager.Instantiate(RegionPrefab);

			DynamicBuffer<SpaceData.RegionBufferElement> regions = GetBufferFromEntity<SpaceData.RegionBufferElement>()[SpaceEntity];

			RegionData.SpatialIndex spatialIndex = new RegionData.SpatialIndex(chunkPos);
			EntityManager.SetComponentData(region, spatialIndex);
			InsertRegion(regions, region);

			Vector2 positionOffset = chunkPos * Space.regionSize;
			positionOffset *= metersPerCell;

			Translation translation = EntityManager.GetComponentData<Translation>(region);
			translation.Value += new float3(positionOffset.x, positionOffset.y, 0f);
			EntityManager.SetComponentData(region, translation);

			foreach (Vector2Int position in Enumerators.GetSquare(Space.chunksPerRegion))
			{
				Entity chunk = EntityManager.Instantiate(ChunkPrefab);

				ChunkData.RegionalIndex regionalIndex = new ChunkData.RegionalIndex(position);
				EntityManager.SetComponentData(chunk, regionalIndex);
				EntityManager.SetComponentData(chunk, new ChunkData.SpatialIndex(spatialIndex, regionalIndex));

				EntityManager.SetSharedComponentData(chunk, new ChunkData.ProcessingBatchIndex(position));

				var atoms = EntityManager.GetBuffer<ChunkData.AtomBufferElement>(chunk);
				for (int i = 0; i < Space.chunkSize * Space.chunkSize; i++)
					atoms.Add(Entity.Null);

				EntityManager.GetBuffer<RegionData.ChunkBufferElement>(region).Add(chunk);
			}

			EntityManager.SetSharedComponentData(region,
				new RegionData.Processing
				{ state = RegionData.Processing.State.PendingGeneration }
			);
		}

		public void InsertRegion(DynamicBuffer<SpaceData.RegionBufferElement> regions, Entity region)
		{
			RegionData.SpatialIndex position = EntityManager.GetComponentData<RegionData.SpatialIndex>(region);

			RectInt rect = GetSingleton<SpaceData.Bounds>().spaceGridBounds;
			rect.SetMinMax(
				Vector2Int.Min(rect.min, position.index),
				Vector2Int.Max(rect.max, position.index)
			);
			SetSingleton(new SpaceData.Bounds() { spaceGridBounds = rect });

			int width = rect.width;

			int weight = position.GetSortingWeight(width);

			int length = regions.Length;
			if (length == 0)
			{
				regions.Add(region);
				return;
			}

			int count = 0;
			while (
				EntityManager.GetComponentData<RegionData.SpatialIndex>(regions[count++]).GetSortingWeight(width) < weight &&
				count < regions.Length
			)
				;

			regions.Insert(count, region);
		}
	}
}