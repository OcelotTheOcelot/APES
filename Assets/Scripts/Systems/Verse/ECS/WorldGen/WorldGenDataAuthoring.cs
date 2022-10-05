using Unity.Entities;
using Unity.Collections;
using UnityEngine;

namespace Verse
{
    public class WorldGenDataAuthoring : MonoBehaviour
    {
        public int terrainHeight;
        public int hillsHeight;

        public GameObject soilMatter;
        public GameObject graniteMatter;
        public GameObject waterMatter;
    }

    public struct TerrainGenerationData : IComponentData
	{
		public int terrainHeight;
		public int hillsHeight;

		public Entity soilMatter;
		public Entity graniteMatter;
		public Entity waterMatter;
	}

	public class TerrainGenerationAuthoringBaker : Baker<WorldGenDataAuthoring>
	{
		public override void Bake(WorldGenDataAuthoring authoring)
		{
			AddComponent(new TerrainGenerationData
				{
					terrainHeight = authoring.terrainHeight,
					hillsHeight = authoring.hillsHeight,

					soilMatter = GetEntity(authoring.soilMatter),
					graniteMatter = GetEntity(authoring.graniteMatter),
					waterMatter = GetEntity(authoring.waterMatter)
				}
			);
		}
	}
}