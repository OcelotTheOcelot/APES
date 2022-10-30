using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace Verse
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public class VerseTickSystemGroup : ComponentSystemGroup
	{
		protected override void OnUpdate() { }

		public void Tick() => base.OnUpdate();
	}
}
