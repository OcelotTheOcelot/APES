using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using Unity.Entities;
using UnityEngine;


namespace Verse
{
	[GenerateAuthoringComponent]
	public struct RegionTextureParent : IComponentData
	{
		public Entity region;
	}
}