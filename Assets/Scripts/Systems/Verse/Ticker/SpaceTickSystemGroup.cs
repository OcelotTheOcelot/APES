using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace Verse
{
	public class WorldTickSystemGroup : ComponentSystemGroup
	{
		protected override void OnUpdate() { }

		public void Tick() => base.OnUpdate();
	}
}
