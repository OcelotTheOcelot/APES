using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using Unity.Entities;
using UnityEngine;


namespace Verse
{
	public class RegionTextureAuthoring : MonoBehaviour
	{
		public GameObject region;

        public class Baker : Baker<RegionTextureAuthoring>
        {
            public override void Bake(RegionTextureAuthoring authoring)
            {
                AddComponent(new RegionTexture.OwningRegion { region = GetEntity(authoring.region) });
                AddSharedComponent(new RegionTexture.Processing(false));
            }
        }
    }
}