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

		// Guess this definition has to be relocated somewhere else
		private EntityArchetype chunkArchetype;

		private Entity space;

		protected override void OnCreate()
		{
			base.OnCreate();

			chunkArchetype = EntityManager.CreateArchetype(
				ComponentType.ReadWrite<Chunk.Region>(),
				ComponentType.ReadWrite<Chunk.RegionalIndex>(),
				ComponentType.ReadWrite<Chunk.SpatialIndex>(),
				ComponentType.ReadWrite<Chunk.DirtyArea>(),
				ComponentType.ReadWrite<Chunk.ColliderStatus>(),
				ComponentType.ReadWrite<Chunk.Neighbourhood>(),
				ComponentType.ReadWrite<Chunk.ProcessingBatchIndex>(),
				ComponentType.ReadWrite<Chunk.AtomBufferElement>(),
				ComponentType.ReadWrite<PolygonCollider2D>()
			);
		}

		protected override void OnStartRunning()
		{
			base.OnStartRunning();

			space = GetSingletonEntity<Space.Tag>();

			Coord regionCount = GetSingleton<Space.Initialization>().regionCount;
			foreach (Coord chunkPos in Enumerators.GetRect(regionCount))
				InstantiateRegion(chunkPos);

			Debug.Log($"Space of {regionCount.x} by {regionCount.y} chunks has been created.");
		}

		protected override void OnUpdate() {}

		private void InstantiateRegion(Coord regionPos)
		{
			Entity region = EntityManager.Instantiate(Prefabs.Region);

			DynamicBuffer<Space.RegionBufferElement> regions = EntityManager.GetBuffer<Space.RegionBufferElement>(space);
			// GetBufferLookup<Space.RegionBufferElement>()[SpaceEntity];

			Region.SpatialIndex spatialIndex = new(regionPos);
			EntityManager.SetComponentData(region, spatialIndex);

			InsertRegion(regions, region, regionPos);

			float2 positionOffset = regionPos * Space.regionSize;
			positionOffset *= Space.metersPerCell;

			LocalToWorldTransform transform = EntityManager.GetComponentData<LocalToWorldTransform>(region);
			transform.Value.Position += new float3(positionOffset.x, positionOffset.y, 0f);
			transform.Value.Scale *= defaultPixelsPerMeter * Space.metersPerCell;
			EntityManager.SetComponentData(region, transform);

			EntityManager.SetSharedComponentManaged(region,
				new Region.Processing
				{ state = Region.Processing.State.PendingGeneration }
			);

			Entity[] newChunks = new Entity[Space.chunksPerRegion * Space.chunksPerRegion];
			int chunkCount = 0;
			foreach (Coord regionalPos in Enumerators.GetSquare(Space.chunksPerRegion))
			{
				Entity chunk = EntityManager.Instantiate(Prefabs.Chunk);
				// Entity chunk = EntityManager.CreateEntity(chunkArchetype);

				newChunks[chunkCount++] = chunk;

				EntityManager.SetComponentData(chunk, new Chunk.Region() { region = region });

				bool hasComp = EntityManager.HasComponent<PolygonCollider2D>(chunk);
				var polCol2d = EntityManager.GetComponentObject<PolygonCollider2D>(chunk);
				Debug.Log($"Collider: {hasComp}; {polCol2d != null}");

				Chunk.RegionalIndex regionalIndex = new(regionalPos);

				EntityManager.SetComponentData(chunk, regionalIndex);
				EntityManager.SetComponentData(chunk, new Chunk.SpatialIndex(spatialIndex, regionalIndex));

				EntityManager.SetSharedComponentManaged(chunk, new Chunk.ProcessingBatchIndex(regionalPos));

				var atoms = EntityManager.GetBuffer<Chunk.AtomBufferElement>(chunk);
				for (int i = 0; i < Space.chunkSize * Space.chunkSize; i++)
					atoms.Add(Entity.Null);

				// Don't you event think about relocating this line unless you're transferring the system to ECB.
				DynamicBuffer<Region.ChunkBufferElement> chunkBuffer = EntityManager.GetBuffer<Region.ChunkBufferElement>(region);
				chunkBuffer.Add(chunk);

				LinkInnerNeighbours(chunk, chunkBuffer, regionalPos);
			}

			LinkOuterNeighbours(regionPos, newChunks);
		}

		private void LinkOuterNeighbours(Coord regionPos, Entity[] newChunks)
		{
			int lastRowFirstIndex = Space.chunksPerRegion * (Space.chunksPerRegion - 1);

			// Linking regions neighbouring horizontally
			if (Space.GetRegionByIndex(EntityManager, space, regionPos + Coord.west, out Entity westernRegion))
			{
				for (int thisRegionChunkIndex = 0; thisRegionChunkIndex <= lastRowFirstIndex; thisRegionChunkIndex += Space.chunksPerRegion)
				{
					int westernRegionChunkIndex = thisRegionChunkIndex + Space.chunksPerRegion - 1;

					Entity thisChunk = newChunks[thisRegionChunkIndex];
					Chunk.Neighbourhood thisNeighbourhood = EntityManager.GetComponentData<Chunk.Neighbourhood>(thisChunk);

					Entity westernChunk = Region.GetChunkByIndexNonSafe(EntityManager, westernRegion, westernRegionChunkIndex);
					Chunk.Neighbourhood westernNeighbourhood = EntityManager.GetComponentData<Chunk.Neighbourhood>(westernChunk);

					thisNeighbourhood.West = westernChunk;
					westernNeighbourhood.East = thisChunk;
					EntityManager.SetComponentData(westernChunk, westernNeighbourhood);

					if (thisRegionChunkIndex < lastRowFirstIndex)
					{
						int northWesternChunkIndex = westernRegionChunkIndex + Space.chunksPerRegion;
						Entity northWesternChunk = Region.GetChunkByIndexNonSafe(EntityManager, westernRegion, northWesternChunkIndex);

						Chunk.Neighbourhood northWesternNeighbourhood = EntityManager.GetComponentData<Chunk.Neighbourhood>(northWesternChunk);

						thisNeighbourhood.NorthWest = northWesternChunk;
						northWesternNeighbourhood.SouthEast = thisChunk;

						EntityManager.SetComponentData(northWesternChunk, northWesternNeighbourhood);
					}
					if (thisRegionChunkIndex > 0)
					{
						int southWesternChunkIndex = westernRegionChunkIndex - Space.chunksPerRegion;
						Entity southWesternChunk = Region.GetChunkByIndexNonSafe(EntityManager, westernRegion, southWesternChunkIndex);

						Chunk.Neighbourhood southWesternNeighbourhood = EntityManager.GetComponentData<Chunk.Neighbourhood>(southWesternChunk);

						thisNeighbourhood.SouthWest = southWesternChunk;
						southWesternNeighbourhood.NorthEast = thisChunk;

						EntityManager.SetComponentData(southWesternChunk, southWesternNeighbourhood);
					}

					EntityManager.SetComponentData(thisChunk, thisNeighbourhood);
				}
			}

			if (Space.GetRegionByIndex(EntityManager, space, regionPos + Coord.south, out Entity southernRegion))
			{
				for (int thisRegionChunkIndex = 0; thisRegionChunkIndex < Space.chunksPerRegion; thisRegionChunkIndex++)
				{
					int southernRegionChunkIndex = lastRowFirstIndex + thisRegionChunkIndex;

					Entity thisChunk = newChunks[thisRegionChunkIndex];
					Chunk.Neighbourhood thisNeighbourhood = EntityManager.GetComponentData<Chunk.Neighbourhood>(thisChunk);

					Entity southernChunk = Region.GetChunkByIndexNonSafe(EntityManager, southernRegion, southernRegionChunkIndex);
					Chunk.Neighbourhood southernNeighbourhood = EntityManager.GetComponentData<Chunk.Neighbourhood>(southernChunk);

					thisNeighbourhood.South = southernChunk;
					southernNeighbourhood.North = thisChunk;
					EntityManager.SetComponentData(southernChunk, southernNeighbourhood);

					if (thisRegionChunkIndex > 0)
					{
						int southWesternChunkIndex = southernRegionChunkIndex - 1;
						Entity southWesternChunk = Region.GetChunkByIndexNonSafe(EntityManager, southernRegion, southWesternChunkIndex);

						Chunk.Neighbourhood southWesternNeighbourhood = EntityManager.GetComponentData<Chunk.Neighbourhood>(southWesternChunk);

						thisNeighbourhood.SouthWest = southWesternChunk;
						southWesternNeighbourhood.NorthEast = thisChunk;

						EntityManager.SetComponentData(southWesternChunk, southWesternNeighbourhood);
					}
					if (thisRegionChunkIndex < Space.chunksPerRegion - 1)
					{
						int southEasternChunkIndex = southernRegionChunkIndex + 1;
						Entity southEasternChunk = Region.GetChunkByIndexNonSafe(EntityManager, southernRegion, southEasternChunkIndex);

						Chunk.Neighbourhood southEasternNeighbourhood = EntityManager.GetComponentData<Chunk.Neighbourhood>(southEasternChunk);

						thisNeighbourhood.SouthEast = southEasternChunk;
						southEasternNeighbourhood.NorthWest = thisChunk;

						EntityManager.SetComponentData(southEasternChunk, southEasternNeighbourhood);
					}

					EntityManager.SetComponentData(thisChunk, thisNeighbourhood);
				}
			}

			if (Space.GetRegionByIndex(EntityManager, space, regionPos + Coord.southWest, out Entity southWesternRegion))
			{
				Entity thisChunk = newChunks[0];
				Chunk.Neighbourhood thisNeighbourhood = EntityManager.GetComponentData<Chunk.Neighbourhood>(thisChunk);

				Entity southWesternChunk = Region.GetChunkByIndexNonSafe(EntityManager, southWesternRegion, newChunks.Length - 1);
				Chunk.Neighbourhood southWesternNeighbourhood = EntityManager.GetComponentData<Chunk.Neighbourhood>(southWesternChunk);

				thisNeighbourhood.SouthWest = southWesternChunk;
				southWesternNeighbourhood.NorthEast = thisChunk;
				EntityManager.SetComponentData(southWesternChunk, southWesternNeighbourhood);
				EntityManager.SetComponentData(thisChunk, thisNeighbourhood);
			}
		}

		private void LinkInnerNeighbours(Entity chunk, DynamicBuffer<Region.ChunkBufferElement> chunks, Coord regionalPos)
		{
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

				if (regionalPos.x < Space.chunksPerRegion - 1)
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

		public void InsertRegion(DynamicBuffer<Space.RegionBufferElement> regions, Entity region, Coord spatialPos)
		{
			CoordRect rect = GetSingleton<Space.Bounds>().spaceGridBounds;
			rect.Set(
				Coord.Min(rect.min, spatialPos),
				Coord.Max(rect.max, spatialPos)
			);
			SetSingleton(new Space.Bounds() { spaceGridBounds = rect });

			int width = rect.Width;
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