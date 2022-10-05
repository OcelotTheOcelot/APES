using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using Verse;

public partial class TickerSystem : SystemBase
{
	private static float nextTick;
	private static float secondsPerTick;
	public static int CurrentTick { get; private set; }

	private static WorldTickSystemGroup group;

	protected override void OnStartRunning()
	{
		base.OnStartRunning();

		group = World.GetExistingSystemManaged<WorldTickSystemGroup>();
		secondsPerTick = 1f / GetSingleton<TickRate>().ticksPerSecond;
	}

	protected override void OnUpdate()
	{
		float time = (float)World.Time.ElapsedTime;

		if (time < nextTick)
			return;

		nextTick = time + secondsPerTick;
		Tick();
	}

	private void Tick()
	{
		CurrentTick++;
		Apes.UI.DebugUI.Instance.TickText = $"Tick #{CurrentTick}";

		group.Tick();
	}
}
