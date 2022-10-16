using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.XR.OpenVR;
using UnityEngine;
using Verse;

namespace Verse
{
	public partial class TickerSystemGroup : ComponentSystemGroup
	{
		protected override void OnCreate()
		{
			base.OnCreate();

			RequireForUpdate<TickerSettings>();
		}

		protected override void OnStartRunning()
		{
			base.OnStartRunning();

			TickerSettings settings = GetSingleton<TickerSettings>();

			var limitedTicker = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<LimitedTickerSystem>();
			var compensatingTicker = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<CompensatingTickerSystem>();
			var immediateTicker = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<ImmediatelyCompensatingTickerSystem>();
			var unlimitedTicker = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<UnlimitedTickerSystem>();

			switch (settings.mode)
			{
				case TickerSettings.Mode.limited:
					limitedTicker.Enabled = true;
					compensatingTicker.Enabled = false;
					immediateTicker.Enabled = false;
					unlimitedTicker.Enabled = false;
					break;
				case TickerSettings.Mode.compensating:
					limitedTicker.Enabled = false;
					compensatingTicker.Enabled = true;
					immediateTicker.Enabled = false;
					unlimitedTicker.Enabled = false;
					break;
				case TickerSettings.Mode.immediatelyCompensating:
					limitedTicker.Enabled = false;
					compensatingTicker.Enabled = false;
					immediateTicker.Enabled = true;
					unlimitedTicker.Enabled = false;
					break;
				case TickerSettings.Mode.unlimited:
					limitedTicker.Enabled = false;
					compensatingTicker.Enabled = false;
					immediateTicker.Enabled = false;
					unlimitedTicker.Enabled = true;
					break;
			}
		}

		[UpdateInGroup(typeof(TickerSystemGroup))]
		private partial class LimitedTickerSystem : TickerSystem
		{
			private float nextTick;
			private float secondsPerTick;

			protected override void OnStartRunning()
			{
				base.OnStartRunning();

				TickerSettings settings = GetSingleton<TickerSettings>();
				secondsPerTick = 1f / settings.ticksPerSecond;
			}

			protected override void OnUpdate()
			{
				float time = (float)World.Time.ElapsedTime;

				if (time < nextTick)
					return;

				nextTick = time + secondsPerTick;
				Tick();
			}
		}

		[UpdateInGroup(typeof(TickerSystemGroup))]
		private partial class CompensatingTickerSystem : TickerSystem
		{
			private float lastUnpausingTime;
			private float ticksPerSecond;
			private int ticksShouldHavePassed;
			private int passedTicks;

			protected override void OnStartRunning()
			{
				base.OnStartRunning();

				lastUnpausingTime = (float)World.Time.ElapsedTime;

				TickerSettings settings = GetSingleton<TickerSettings>();
				ticksPerSecond = settings.ticksPerSecond;
			}

			protected override void OnUpdate()
			{
				float time = (float)World.Time.ElapsedTime;
				ticksShouldHavePassed = Mathf.FloorToInt((time - lastUnpausingTime) * ticksPerSecond);

				if (passedTicks >= ticksShouldHavePassed)
					return;

				passedTicks++;
				Tick();
			}
		}

		[UpdateInGroup(typeof(TickerSystemGroup))]
		private partial class ImmediatelyCompensatingTickerSystem : TickerSystem
		{
			private float lastUnpausingTime;
			private float ticksPerSecond;
			private int ticksShouldHavePassed;
			private int passedTicks;

			protected override void OnStartRunning()
			{
				base.OnStartRunning();

				lastUnpausingTime = (float)World.Time.ElapsedTime;

				TickerSettings settings = GetSingleton<TickerSettings>();
				ticksPerSecond = settings.ticksPerSecond;
			}

			protected override void OnUpdate()
			{
				float time = (float)World.Time.ElapsedTime;
				ticksShouldHavePassed = Mathf.FloorToInt((time - lastUnpausingTime) * ticksPerSecond);

				while (passedTicks < ticksShouldHavePassed)
				{
					passedTicks++;
					Tick();
				}
			}
		}

		[UpdateInGroup(typeof(TickerSystemGroup))]
		private partial class UnlimitedTickerSystem : TickerSystem
		{
			protected override void OnUpdate()
			{
				Tick();
			}
		}
	}

	public abstract partial class TickerSystem : SystemBase
	{
		private static WorldTickSystemGroup group;

		public static int CurrentTick { get; private set; }

		protected override void OnCreate()
		{
			base.OnCreate();

			RequireForUpdate<Space.Tag>();
			
			group = World.GetExistingSystemManaged<WorldTickSystemGroup>();
		}

		protected void Tick()
		{
			CurrentTick++;
			Apes.UI.DebugUI.Instance.TickText = $"Tick #{CurrentTick}";

			group.Tick();
		}
	}
}
