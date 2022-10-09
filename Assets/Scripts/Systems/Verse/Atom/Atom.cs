using UnityEngine;
using Unity.Entities;
using Unity.Collections;

namespace Verse
{
	public static class Atom
	{
		public struct Matter : IComponentData
		{
			[ReadOnly]
			public Entity matter;

			public Matter(Entity matter)
			{
				this.matter = matter;
			}
		}

		public struct Color : IComponentData
		{
			public Color32 color;
			public Color(Color32 color)
			{
				this.color = color;
			}
		}

		public struct Temperature : IComponentData
		{
			public float temperature;
			public Temperature(float temperature)
			{
				this.temperature = temperature;
			}
		}
		
		public static Entity GetMatter(EntityManager dstManager, Entity atom) =>
			dstManager.GetComponentData<Matter>(atom).matter;
	}
}
