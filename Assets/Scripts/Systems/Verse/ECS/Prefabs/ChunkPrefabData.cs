using UnityEngine;
using Unity.Entities;

namespace Verse
{
	[GenerateAuthoringComponent]
	public struct ChunkPrefabData : IComponentData
	{
		public Entity prefab;
	}
}
