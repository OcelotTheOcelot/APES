using UnityEngine;
using Unity.Entities;
using System;

using static Verse.ChunkData;
using System.Runtime.CompilerServices;

namespace Verse
{
	public static class Chunk
	{
		//public static bool CreateAtom(EntityManager dstManager, Entity chunk, Entity matterPrefab, Vector2Int chunkCoord)
		//{
		//	Entity atom = dstManager.Instantiate(SpaceInitializationSystem.AtomPrefab);

		//	dstManager.SetComponentData(atom, new AtomData.Matter { matterPrefab = matterPrefab });

		//	MatterData.Creation creationData = dstManager.GetComponentData<MatterData.Creation>(matterPrefab);
		//	dstManager.SetComponentData(atom, new AtomData.Temperature { temperature = creationData.defaultTemperature });

		//	var colors = dstManager.GetBuffer<MatterData.ColorVariantBufferElement>(matterPrefab);
		//	dstManager.SetComponentData(atom, new AtomData.Color
		//		{
		//			color = colors.Length > 0 ? Utils.Pick(colors) : MatterData.invalidColor
		//		}
		//	);

		//	var atoms = dstManager.GetBuffer<AtomBufferElement>(chunk);
		//	atoms.SetAtom(chunkCoord.x, chunkCoord.y, atom);

		//	return true;
		//}

		public static bool CreateAtom(EntityManager dstManager, Entity chunk, Entity matterPrefab, Vector2Int chunkCoord)
		{
			Entity atom = dstManager.Instantiate(SpaceInitializationSystem.AtomPrefab);

			dstManager.SetComponentData(atom, new AtomData.Matter { matterPrefab = matterPrefab });

			MatterData.Creation creationData = dstManager.GetComponentData<MatterData.Creation>(matterPrefab);
			dstManager.SetComponentData(atom, new AtomData.Temperature { temperature = creationData.temperature });

			dstManager.SetComponentData<AtomData.Color>(atom,
				Utils.Pick(dstManager.GetBuffer<MatterData.ColorBufferElement>(matterPrefab))
			);

			var atoms = dstManager.GetBuffer<AtomBufferElement>(chunk);
			atoms.SetAtom(chunkCoord, atom);

			return true;
		}

		public static Entity GetAtom(EntityManager dstManager, Entity chunk, Vector2Int chunkCoord) =>
			GetAtom(dstManager, chunk, chunkCoord.x, chunkCoord.y);
		public static Entity GetAtom(EntityManager dstManager, Entity chunk, int chunkCoordX, int chunkCoordY) =>
			dstManager.GetBuffer<AtomBufferElement>(chunk).GetAtom(chunkCoordX, chunkCoordY);

		public static void MarkDirty(EntityManager dstManager, Entity chunk, RectInt rect, bool safe = true)
		{
			DirtyArea area = dstManager.GetComponentData<DirtyArea>(chunk);
			area.MarkDirty(rect, safe: safe);
			dstManager.SetComponentData(chunk, area);
		}

		public static void MarkDirty(EntityManager dstManager, Entity chunk)
		{
			DirtyArea area = dstManager.GetComponentData<DirtyArea>(chunk);
			area.MarkDirty();
			dstManager.SetComponentData(chunk, area);
		}
	}
}
