using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using Unity.Entities;
using UnityEngine;


namespace Verse
{
	public class RegionTextureDataAuthoring : MonoBehaviour
	{
		public GameObject region;
	}

	public class RegionTextureDataAuthoringBaker : Baker<RegionTextureDataAuthoring>
	{
		public override void Bake(RegionTextureDataAuthoring authoring)
		{
			AddComponent(new RegionTexture.OwningRegion { region = GetEntity(authoring.region) });
		}
	}
}