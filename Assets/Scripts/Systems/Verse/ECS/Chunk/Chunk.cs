using UnityEngine;
using Unity.Entities;
using System;
using System.Collections.Generic;

namespace Verse
{
	public static partial class Chunk
	{
		public struct RegionalIndex : IComponentData
		{
			public Vector2Int index;
			public Vector2Int origin;

			public RegionalIndex(Vector2Int position)
			{
				index = position;
				origin = position * Space.chunkSize;
			}
		}

		public struct SpatialIndex : IComponentData
		{
			public Vector2Int origin;

			public SpatialIndex(Verse.Region.SpatialIndex regionIndex, RegionalIndex index)
			{
				origin = regionIndex.origin + index.origin;
			}
		}

		public struct Region : IComponentData
		{
			public Entity region;
		}

		[InternalBufferCapacity(64)]
		public struct AtomBufferElement : IBufferElementData
		{
			public Entity atom;

			public static implicit operator Entity(AtomBufferElement bufferElement) => bufferElement.atom;
			public static implicit operator AtomBufferElement(Entity atom) => new()
			{
				atom = atom
			};
		}

		/// <summary>
		/// Determines order in which clusters should be processed.
		/// Clusters with the same number can be safely processed simultaneously.
		/// I'm lazy so I'll just illustrate:
		/// 
		/// 0 1 0 1 0 1
		/// 2 3 2 3 2 3
		/// 0 1 0 1 0 1
		/// 2 3 2 3 2 3
		/// 
		/// So that chunks with same index can be processed at the same time.
		/// </summary>
		public struct ProcessingBatchIndex : ISharedComponentData
		{
			public int batchIndex;

			public ProcessingBatchIndex(int batchIndex)
			{
				this.batchIndex = batchIndex;
			}

			public ProcessingBatchIndex(Vector2Int gridPos)
			{
				batchIndex = GetIndexFromGridPos(gridPos);
			}

			public static int GetIndexFromGridPos(Vector2Int gridPos) => GetIndexFromGridPos(gridPos.x & 0b1, gridPos.y & 0b1);
			public static int GetIndexFromGridPos(int gridXOddity, int gridYOddity) => (gridYOddity << 1) + gridXOddity;
		}

		public static bool CreateAtom(EntityManager dstManager, Entity chunk, Entity matterPrefab, Vector2Int chunkCoord)
		{
			Entity atom = dstManager.CreateEntity(Archetypes.Atom);

			dstManager.SetComponentData(atom, new Atom.Matter { matter = matterPrefab });

			Matter.Creation creationData = dstManager.GetComponentData<Matter.Creation>(matterPrefab);
			dstManager.SetComponentData(atom, new Atom.Temperature { temperature = creationData.temperature });

			dstManager.SetComponentData<Atom.Color>(atom,
				Utils.Pick(dstManager.GetBuffer<Matter.ColorBufferElement>(matterPrefab))
			);

			var atoms = dstManager.GetBuffer<AtomBufferElement>(chunk);
			atoms.SetAtom(chunkCoord, atom);

			return true;
		}

		public static Entity GetAtom(EntityManager dstManager, Entity chunk, Vector2Int chunkCoord) =>
			GetAtom(dstManager, chunk, chunkCoord.x, chunkCoord.y);
		public static Entity GetAtom(EntityManager dstManager, Entity chunk, int chunkCoordX, int chunkCoordY) =>
			dstManager.GetBuffer<AtomBufferElement>(chunk).GetAtom(chunkCoordX, chunkCoordY);

		internal static bool RemoveAtom(EntityManager dstManager, Entity chunk, Vector2Int chunkCoord)
		{
			var atoms = dstManager.GetBuffer<AtomBufferElement>(chunk);
			// dstManager.DestroyEntity(atoms.GetAtom(chunkCoord));
			atoms.SetAtom(chunkCoord, Entity.Null);

			return true;
		}
	}
}
