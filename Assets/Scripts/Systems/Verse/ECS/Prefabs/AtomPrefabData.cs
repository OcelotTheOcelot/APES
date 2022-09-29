using UnityEngine;
using Unity.Entities;

namespace Verse
{
	[GenerateAuthoringComponent]
	public struct AtomPrefabData : IComponentData
	{
		public Entity prefab;
	}
}
