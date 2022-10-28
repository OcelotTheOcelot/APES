using System;
using Unity.Entities;
using UnityEngine;

namespace Verse
{
	public class TickerSettingsAuthoring : MonoBehaviour
	{
		public TickerSettings tickerSettings;

		public class Baker : Baker<TickerSettingsAuthoring>
		{
			public override void Bake(TickerSettingsAuthoring authoring)
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