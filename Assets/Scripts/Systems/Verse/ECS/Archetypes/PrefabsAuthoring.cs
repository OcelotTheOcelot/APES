using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Verse
{
	public class PrefabsAuthoring : MonoBehaviour
	{
		// [SerializeField]
		// private GameObject atomPrefab;
		// [SerializeField]
		// private GameObject chunkPrefab;
		[SerializeField]
		private GameObject regionPrefab;

		public class Baker : Baker<PrefabsAuthoring>
		{
			public override void Bake(PrefabsAuthoring authoring)
			{
				// Entity atom = GetEntity(authoring.atomPrefab);
				// Entity chunk = GetEntity(authoring.chunkPrefab);
				Entity region = GetEntity(authoring.regionPrefab);

				// AddComponent(new Prefabs.AtomPrefab { prefab = atom });
				// AddComponent(new Prefabs.ChunkPrefab { prefab = chunk });
				AddComponent(new Prefabs.RegionPrefab { prefab = region });
			}
		}
	}
}