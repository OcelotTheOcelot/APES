using UnityEngine;
using Unity.Entities;
using Unity.Collections;
using Unity.Transforms;
using Unity.Mathematics;

namespace Verse
{
	[UpdateInGroup(typeof(WorldTickSystemGroup), OrderLast = true)]
	public partial class GizmoSystem : SystemBase
	{
		private float metersPerCell;
		private int regionSize;
		private int chunkSize;

		private float tickDuration;

		EntityQuery regionQuery;
		EntityQuery chunkQuery;

		protected override void OnCreate()
		{
			base.OnCreate();
			
			regionQuery = GetEntityQuery(
				ComponentType.ReadOnly<RegionData.SpatialIndex>(),
				ComponentType.ReadOnly<Translation>()
			);

			chunkQuery = GetEntityQuery(
				ComponentType.ReadOnly<ChunkData.DirtyArea>()
			);
		}

		protected override void OnStartRunning()
		{
			base.OnStartRunning();

			SpaceData.Size sizeData = GetSingleton<SpaceData.Size>();
			metersPerCell = 1f / sizeData.cellsPerMeter;
			regionSize = sizeData.regionSize;
			chunkSize = sizeData.chunkSize;

			tickDuration = 1f / GetSingleton<TickRateData>().ticksPerSecond;
		}

		protected override void OnUpdate()
		{
			new DrawRegionGizmosJob
			{
				chunkBorderColor = new Color(.8f, .5f, 0f, .5f),
				regionBorderColor = new Color(1f, 1f, 0f, .5f),

				duration = tickDuration,
				regionSize = regionSize,
				chunkSize = chunkSize,
				metersPerCell = metersPerCell
			}.Run(regionQuery);
			
			new DrawDirtyAreaGizmosJob
			{
				dirtyAreaColor = new Color(1f, 0f, 0f, .5f),
				metersPerCell = metersPerCell,
				duration = tickDuration
			}.Run(chunkQuery);
		}

		public partial struct DrawRegionGizmosJob : IJobEntity
		{
			[ReadOnly]
			public int regionSize;

			[ReadOnly]
			public int chunkSize;

			[ReadOnly]
			public float duration;
			
			[ReadOnly]
			public float metersPerCell;

			[ReadOnly]
			public Color chunkBorderColor;

			[ReadOnly]
			public Color regionBorderColor;
			
			public void Execute(in Translation translation)
			{
				Vector3 origin = translation.Value;
				Vector3 size = new (regionSize * metersPerCell, regionSize * metersPerCell);

				Vector3 rightBorderShift = size.x * Vector3.right;
				Vector3 upperBorderShift = size.y * Vector3.up;

				for (int y = chunkSize; y < regionSize; y += chunkSize)
				{
					Vector3 line = origin + y * metersPerCell * Vector3.up;
					Debug.DrawLine(line, line + rightBorderShift, chunkBorderColor, duration: duration);
				}

				for (int x = chunkSize; x < regionSize; x += chunkSize)
				{
					Vector3 line = origin + x * metersPerCell * Vector3.right;
					Debug.DrawLine(line, line + upperBorderShift, chunkBorderColor, duration: duration);
				}

				Debug.DrawLine(origin, origin + rightBorderShift, regionBorderColor, duration: duration);
				Debug.DrawLine(origin, origin + upperBorderShift, regionBorderColor, duration: duration);
				Debug.DrawLine(origin + size, origin + rightBorderShift, regionBorderColor, duration: duration);
				Debug.DrawLine(origin + size, origin + upperBorderShift, regionBorderColor, duration: duration);
			}
		}

		public partial struct DrawDirtyAreaGizmosJob : IJobEntity
		{
			[ReadOnly]
			public float metersPerCell;

			[ReadOnly]
			public Color dirtyAreaColor;

			[ReadOnly]
			public float duration;

			public void Execute(in ChunkData.SpatialIndex index, in ChunkData.DirtyArea area)
			{
				if (!area.active)
					return;

				float margin = .25f;

				Vector2 size = area.Size - margin * 2 * Vector2.one;
				size *= metersPerCell;

				Vector2 cornerA = (index.origin + area.from + margin * Vector2.one) * metersPerCell;
				Vector2 cornerB = cornerA + new Vector2(size.x, 0);
				Vector2 cornerC = cornerA + new Vector2(0, size.y);
				Vector2 cornerD = cornerA + new Vector2(size.x, size.y);

				Debug.DrawLine(cornerA, cornerB, dirtyAreaColor, duration: duration);
				Debug.DrawLine(cornerA, cornerC, dirtyAreaColor, duration: duration);
				Debug.DrawLine(cornerD, cornerB, dirtyAreaColor, duration: duration);
				Debug.DrawLine(cornerD, cornerC, dirtyAreaColor, duration: duration);
			}
		}

		public partial struct DrawBatchIndexJob : IJobEntity
		{
			[ReadOnly]
			public float metersPerCell;

			[ReadOnly]
			public Color dirtyAreaColor;

			public void Execute(in ChunkData.SpatialIndex index, ref ChunkData.ProcessingBatchIndex batchIndex)
			{
				UnityEditor.Handles.Label((Vector2)(index.origin + new Vector2Int(Space.chunkSize / 2, Space.chunkSize / 2)) * metersPerCell, $"{batchIndex}");
			}
		}
	}
}
