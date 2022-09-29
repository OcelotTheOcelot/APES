using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace Verse
{
	[DisallowMultipleComponent]
	public class AtomData : MonoBehaviour, IConvertGameObjectToEntity
	{
		public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
		{
			dstManager.AddComponentData(entity, new Matter());
			dstManager.AddComponentData(entity, new Color());
			dstManager.AddComponentData(entity, new Temperature());
		}

		public struct Matter : IComponentData
		{
			public Entity matterPrefab;
		}

		public struct Color : IComponentData
		{
			public Color32 color;
		}

		public struct Temperature : IComponentData
		{
			public float temperature;
		}
	}
}