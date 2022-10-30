using Unity.Entities;
using UnityEditor.SceneManagement;
using UnityEngine;

using static Verse.Space;

namespace Verse
{
	public class SpaceDataAuthoring : MonoBehaviour
	{
		[SerializeField]
		private Vector2Int defaultWorldSize;

		public class Baker : Baker<SpaceDataAuthoring>
		{
			public override void Bake(SpaceDataAuthoring authoring)
			{
				AddComponent(new Tag());
				AddComponent(new Space.Bounds());
				AddComponent(new Initialization { regionCount = authoring.defaultWorldSize / regionSize });
				AddBuffer<RegionBufferElement>();
			}
		}
	}
}