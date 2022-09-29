using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;


namespace Verse
{
	[GenerateAuthoringComponent]
	public struct RegionPrefabData : IComponentData
	{
		public Entity prefab;
	}
}