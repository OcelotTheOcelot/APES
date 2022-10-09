using System;
using Unity.Entities;
using UnityEngine;

namespace Verse
{
	public class TickerDataAuthoring : MonoBehaviour
	{
		public float ticksPerSecond;

		public class Baker : Baker<TickerDataAuthoring>
		{
			public override void Bake(TickerDataAuthoring authoring)
			{
				AddComponent(new TickRate { ticksPerSecond = authoring.ticksPerSecond });
			}
		}
	}

	public struct TickRate : IComponentData
	{
		public float ticksPerSecond;  // default is 60f;
	}
}