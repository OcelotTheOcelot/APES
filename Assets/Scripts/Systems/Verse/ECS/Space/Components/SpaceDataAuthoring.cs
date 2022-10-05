using Unity.Entities;
using UnityEditor.SceneManagement;
using UnityEngine;

using static Verse.Space;

namespace Verse
{
	public class SpaceDataAuthoring : MonoBehaviour
	{
		[SerializeField]
		private int regionSize = 512;
		[SerializeField]
		private int chunkSize = 64;
		[SerializeField]
		private float cellsPerMeter = 16f;

		[SerializeField]
		private Color32 emptySpaceColor;

		[SerializeField]
		private Vector2Int defaultRegionCount;

		public class Baker : Baker<SpaceDataAuthoring>
		{
			public override void Bake(SpaceDataAuthoring authoring)
			{
				AddComponent(new Tag());
				AddComponent(
					new Size
					{
						regionSize = authoring.regionSize,
						chunkSize = authoring.chunkSize,
						cellsPerMeter = authoring.cellsPerMeter
					}
				);
				AddComponent(new Space.Bounds());
				AddComponent(new Colors { emptySpaceColor = authoring.emptySpaceColor });
				AddComponent(new Initialization { regionCount = authoring.defaultRegionCount });
				AddBuffer<RegionBufferElement>();
			}
		}
	}
}