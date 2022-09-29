using System;
using Unity.Entities;
using UnityEngine;

namespace Verse
{
	[GenerateAuthoringComponent]
	public struct TickRateData : IComponentData
	{
		public float ticksPerSecond;  // default is 60f;
	}
}