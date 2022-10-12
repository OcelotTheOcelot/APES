using System;
using Unity.Entities;
using UnityEngine;

namespace Verse
{
	public class TickerDataAuthoring : MonoBehaviour
	{
		public TickerSettings tickerSettings;

		public class Baker : Baker<TickerDataAuthoring>
		{
			public override void Bake(TickerDataAuthoring authoring)
			{
				AddComponent(authoring.tickerSettings);
			}
		}
	}

	[Serializable]
	public struct TickerSettings : IComponentData
	{
		public float ticksPerSecond;  // default is 60f;
		public Mode mode;
		
		public enum Mode
		{
			limited = 0,
			unlimited = 1,
			compensating = 2,
			immediatelyCompensating = 3
		}
	}
}