using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public static class RegionTexture
{
	public struct OwningRegion : IComponentData
	{
		public Entity region;
	}
}
