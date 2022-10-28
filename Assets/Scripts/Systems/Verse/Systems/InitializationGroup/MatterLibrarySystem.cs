using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Entities;
using Unity.Entities.Conversion;
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
				All = new[] { ComponentType.ReadOnly<Matter.StringId>() },
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

		public partial struct RegisterMaterialJob : IJobEntity
		{
			public void Execute(Entity matter, in Matter.StringId stringId, ref Matter.RuntimeId id)
			{
				if (MatterLibrary.Add(stringId.value.ToString(), matter, out int newId))
					id.value = newId;
			}
		}
	}
}