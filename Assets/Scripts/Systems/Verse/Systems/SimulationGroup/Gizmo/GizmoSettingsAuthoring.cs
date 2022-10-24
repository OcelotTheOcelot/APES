using System;
using Unity.Entities;
using UnityEditor.Build.Pipeline;
using UnityEngine;

namespace Verse
{
	public class GizmoSettingsAuthoring : MonoBehaviour
	{
		public GizmoSettings settings;

		public class Baker : Baker<GizmoSettingsAuthoring>
		{
			public override void Bake(GizmoSettingsAuthoring authoring)
			{
				AddComponent(authoring.settings);
			}
		}
	}

	[Serializable]
	public struct GizmoSettings : IComponentData
	{
		public bool showNullNeighbours;
		public bool showRegionBorders;
		public bool showChunkBorders;
		public bool showDirtyAreas;
	}
}