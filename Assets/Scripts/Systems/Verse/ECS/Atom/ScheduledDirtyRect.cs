using UnityEngine;
using Unity.Entities;

namespace Verse
{
	public struct ScheduledDirtyRect : IComponentData
	{
		public RectInt rect;

		public ScheduledDirtyRect(RectInt rect)
		{
			this.rect = rect;
		}
	}
}
