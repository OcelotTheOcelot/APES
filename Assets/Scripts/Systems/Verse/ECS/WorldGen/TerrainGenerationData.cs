using Unity.Entities;
using Unity.Collections;
using UnityEngine;

namespace Verse
{
	[GenerateAuthoringComponent]
	public struct TerrainGenerationData : IComponentData
	{
		public int terrainHeight;
		public int hillsHeight;

		public Entity soilMatter;
		public Entity graniteMatter;
		public Entity waterMatter;
	}
}