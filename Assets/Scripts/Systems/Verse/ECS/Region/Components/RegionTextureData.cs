using UnityEngine;
using Unity.Entities;
using Unity.Transforms;

namespace Verse
{
	[DisallowMultipleComponent]
	public class RegionTextureData : MonoBehaviour, IConvertGameObjectToEntity
	{
		public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
		{
			dstManager.AddComponentData(entity, new Scale { Value = 1f });
		}
	}
}
