using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Entities;
using UnityEngine;

namespace Verse
{
	[UpdateInGroup(typeof(InitializationSystemGroup), OrderLast = true)]
	public partial class MatterLibrarySystem : SystemBase
	{
		EntityQuery matterQuery;

		protected override void OnCreate()
		{
			base.OnCreate();

			EntityQueryDesc desc = new()
			{
				All = new[] { ComponentType.ReadOnly<Matter.Id>() },
				Options = EntityQueryOptions.IncludePrefab
			};

			matterQuery = GetEntityQuery(desc); 
		}

		protected override void OnStartRunning()
		{
			base.OnStartRunning();

			new RegisterMaterialJob { }.ScheduleParallel(matterQuery);

			Enabled = false;
		}

		protected override void OnUpdate()
		{
		}

		[BurstCompile]
		public partial struct RegisterMaterialJob : IJobEntity
		{
			public void Execute(Entity matter, in Matter.Id id)
			{
				MatterLibrary.Add(id.id.ToString(), matter);
			}
		}
	}
}