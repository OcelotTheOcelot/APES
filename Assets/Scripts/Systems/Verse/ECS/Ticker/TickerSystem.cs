using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using Verse;

public class TickerSystem : ComponentSystem
{
	private static float nextTick;
	private static float secondsPerTick;
	public static int CurrentTick { get; private set; }

	private static WorldTickSystemGroup group;

	protected override void OnStartRunning()
	{
		base.OnStartRunning();

		group = World.GetExistingSystem<WorldTickSystemGroup>();
		secondsPerTick = 1f / GetSingleton<TickRateData>().ticksPerSecond;
	}

	protected override void OnUpdate()
	{
		float time = (float)Time.ElapsedTime;

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
