using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Verse
{
	public class PrefabsAuthoring : MonoBehaviour
	{
		[SerializeField]
		private GameObject regionPrefab;

		[SerializeField]
		private GameObject chunkPrefab;

		public class Baker : Baker<PrefabsAuthoring>
		{
			public override void Bake(PrefabsAuthoring authoring)
			{
				Entity region = GetEntity(authoring.regionPrefab);
				AddComponent(new Prefabs.RegionPrefab { prefab = region });

				Entity chunk = GetEntity(authoring.chunkPrefab);
				AddComponent(new Prefabs.ChunkPrefab { prefab = chunk });
			}
		}
	}
}