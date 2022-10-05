using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Rendering;

namespace Verse
{
	[UpdateInGroup(typeof(SpaceInitializationSystemGroup), OrderFirst = true)]
	public partial class SpaceInitializationSystem : SystemBase
	{
		private const float defaultPixelsPerMeter = 100f;
		private EntityArchetype chunkArchetype;

		protected override void OnCreate()
		{
			base.OnCreate();

			chunkArchetype = EntityManager.CreateArchetype(
				ComponentType.ReadWrite<Chunk.Region>(),
				ComponentType.ReadWrite<Chunk.RegionalIndex>(),
				ComponentType.ReadWrite<Chunk.SpatialIndex>(),
				ComponentType.ReadWrite<Chunk.DirtyArea>(),
				ComponentType.ReadWrite<Chunk.Neighbourhood>(),
				ComponentType.ReadWrite<Chunk.ProcessingBatchIndex>(),
				ComponentType.ReadWrite<Chunk.AtomBufferElement>()
			);

		}

		protected override void OnStartRunning()
		{
			base.OnStartRunning();

			Space.RegisterSpaceEntity(GetSingletonEntity<Space.Tag>());

			Space.Size sizeData = GetSingleton<Space.Size>();
			Space.RegisterSpaceSizes(sizeData);

			Space.RegisterSpaceSizes(sizeData);

			// innerChunkPositions = new(1, 1, Space.ChunksPerRegion - 1, Space.ChunksPerRegion - 1);

			Vector2Int regionCount = GetSingleton<Space.Initialization>().regionCount;
			foreach (Vector2Int chunkPos in Enumerators.GetRect(regionCount))
				InstantiateRegion(chunkPos);

			Debug.Log($"Space of {regionCount.x} by {regionCount.y} chunks has been created.");
		}

		protected override void OnUpdate() {}

		private void InstantiateRegion(Vector2Int regionPos)
		{
			Entity region = EntityManager.Instantiate(Prefabs.Region);

			DynamicBuffer<Space.RegionBufferElement> regions = EntityManager.GetBuffer<Space.RegionBufferElement>(Space.SpaceEntity);
			// GetBufferLookup<Space.RegionBufferElement>()[SpaceEntity];

			Region.SpatialIndex spatialIndex = new(regionPos);
			EntityManager.SetComponentData(region, spatialIndex);

			InsertRegion(regions, region, regionPos);

			Vector2 positionOffset = regionPos * Space.RegionSize;
			positionOffset *= Space.MetersPerCell;

			LocalToWorldTransform transform = EntityManager.GetComponentData<LocalToWorldTransform>(region);
			transform.Value.Position += new float3(positionOffset.x, positionOffset.y, 0f);
			transform.Value.Scale *= defaultPixelsPerMeter * Space.MetersPerCell;
			EntityManager.SetComponentData(region, transform);

			EntityManager.SetSharedComponentManaged(region,
				new Region.Processing
				{ state = Region.Processing.State.PendingGeneration }
			);

			foreach (Vector2Int regionalPos in Enumerators.GetSquare(Space.ChunksPerRegion))
			{
				Entity chunk = EntityManager.CreateEntity(chunkArchetype);

				Chunk.RegionalIndex regionalIndex = new(regionalPos);

				EntityManager.SetComponentData(chunk, regionalIndex);
				EntityManager.SetComponentData(chunk, new Chunk.SpatialIndex(spatialIndex, regionalIndex));

				EntityManager.SetSharedComponentManaged(chunk, new Chunk.ProcessingBatchIndex(regionalPos));

				var atoms = EntityManager.GetBuffer<Chunk.AtomBufferElement>(chunk);
				for (int i = 0; i < Space.ChunkSize * Space.ChunkSize; i++)
					atoms.Add(Entity.Null);

				// Don't you event think about relocating this line unless you're transferring the system to ECB.
				DynamicBuffer<Region.ChunkBufferElement> chunks = EntityManager.GetBuffer<Region.ChunkBufferElement>(region);
				chunks.Add(chunk);

				// Inner neighbour linking

				bool hasSouthernNeighbour = regionalPos.y > 0;

				Entity western = Entity.Null, southWestern = Entity.Null;
				if (regionalPos.x > 0)
				{
					western = chunks.GetChunk(regionalPos.x - 1, regionalPos.y);
					Chunk.Neighbourhood westernNeighbourhood = EntityManager.GetComponentData<Chunk.Neighbourhood>(western);
					westernNeighbourhood.East = chunk;
					EntityManager.SetComponentData(western, westernNeighbourhood);

					if (hasSouthernNeighbour)
					{
						southWestern = chunks.GetChunk(regionalPos.x - 1, regionalPos.y - 1);
						Chunk.Neighbourhood southWesternNeighbourhood = EntityManager.GetComponentData<Chunk.Neighbourhood>(southWestern);
						southWesternNeighbourhood.NorthEast = chunk;
						EntityManager.SetComponentData(southWestern, southWesternNeighbourhood);
					}
				}

				Entity southern = Entity.Null, southEastern = Entity.Null;
				if (hasSouthernNeighbour)
				{
					southern = chunks.GetChunk(regionalPos.x, regionalPos.y - 1);
					Chunk.Neighbourhood southernNeighbourhood = EntityManager.GetComponentData<Chunk.Neighbourhood>(southern);
					southernNeighbourhood.North = chunk;
					EntityManager.SetComponentData(southern, southernNeighbourhood);

					if (regionalPos.x < Space.ChunksPerRegion - 1)
					{
						southEastern = chunks.GetChunk(regionalPos.x + 1, regionalPos.y - 1);
						Chunk.Neighbourhood southEasternNeighbourhood = EntityManager.GetComponentData<Chunk.Neighbourhood>(southEastern);
						southEasternNeighbourhood.NorthWest = chunk;
						EntityManager.SetComponentData(southEastern, southEasternNeighbourhood);
					}
				}

				EntityManager.SetComponentData(chunk, new Chunk.Neighbourhood()
					{
						West = western,
						SouthWest = southWestern,
						South = southern,
						SouthEast = southEastern
					}
				);

			}

			// Neighbours linking <TODO>
			DynamicBuffer<Region.ChunkBufferElement> thisChunks = EntityManager.GetBuffer<Region.ChunkBufferElement>(region);
			int maxChunkIndex = Space.ChunksPerRegion * Space.ChunksPerRegion - 1;

			if (Space.GetRegionByIndex(EntityManager, Space.SpaceEntity, regionPos + Vector2Int.left, out Entity westernRegion))
			{
				for (int thisRegionChunkIndex = 0; thisRegionChunkIndex < maxChunkIndex; thisRegionChunkIndex += Space.ChunksPerRegion)
				{
					int westernRegionChunkIndex = thisRegionChunkIndex + Space.ChunksPerRegion - 1;

					Entity thisChunk = thisChunks[thisRegionChunkIndex];
					Chunk.Neighbourhood thisNeighbourhood = EntityManager.GetComponentData<Chunk.Neighbourhood>(thisChunk);

					Entity westernChunk = Region.GetChunkByIndexNonSafe(EntityManager, westernRegion, westernRegionChunkIndex);
					Chunk.Neighbourhood westernNeighbourhood = EntityManager.GetComponentData<Chunk.Neighbourhood>(westernChunk);

					thisNeighbourhood.West = westernChunk;
					westernNeighbourhood.East = thisChunk;

					EntityManager.SetComponentData(thisChunk, thisNeighbourhood);
					EntityManager.SetComponentData(westernChunk, westernNeighbourhood);

					if (thisRegionChunkIndex < maxChunkIndex - Space.ChunksPerRegion)
					{
						int northWesternChunkIndex = thisRegionChunkIndex + Space.ChunksPerRegion;
						Entity northWesternChunk = Region.GetChunkByIndexNonSafe(EntityManager, westernRegion, northWesternChunkIndex);

						Chunk.Neighbourhood northWesternNeighbourhood = EntityManager.GetComponentData<Chunk.Neighbourhood>(northWesternChunk);

						thisNeighbourhood.NorthWest = northWesternChunk;
						northWesternNeighbourhood.SouthEast = thisChunk;

						EntityManager.SetComponentData(northWesternChunk, northWesternNeighbourhood);
					}
					if (thisRegionChunkIndex > 0)
					{
						int southWesternChunkIndex = thisRegionChunkIndex - Space.ChunksPerRegion;
						Entity southWesternChunk = Region.GetChunkByIndexNonSafe(EntityManager, westernRegion, southWesternChunkIndex);

						Chunk.Neighbourhood southWesternNeighbourhood = EntityManager.GetComponentData<Chunk.Neighbourhood>(southWesternChunk);

						thisNeighbourhood.SouthWest = southWesternChunk;
						southWesternNeighbourhood.NorthEast = thisChunk;

						EntityManager.SetComponentData(southWesternChunk, southWesternNeighbourhood);
					}
				}
			}
		}



		public void InsertRegion(DynamicBuffer<Space.RegionBufferElement> regions, Entity region, Vector2Int spatialPos)
		{
			RectInt rect = GetSingleton<Space.Bounds>().spaceGridBounds;
			rect.SetMinMax(
				Vector2Int.Min(rect.min, spatialPos),
				Vector2Int.Max(rect.max, spatialPos)
			);
			SetSingleton(new Space.Bounds() { spaceGridBounds = rect });

			int width = rect.width;

			int weight = Region.SpatialIndex.GetSortingWeight(spatialPos, width);

			int length = regions.Length;
			if (length == 0)
			{
				regions.Add(region);
				return;
			}

			int count = 0;
			while (
				EntityManager.GetComponentData<Region.SpatialIndex>(regions[count++]).GetSortingWeight(width) < weight &&
				count < regions.Length
			)
				;

			regions.Insert(count, region);
		}
	}
}