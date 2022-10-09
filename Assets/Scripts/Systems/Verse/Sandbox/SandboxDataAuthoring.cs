using System;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Verse;

[DisallowMultipleComponent]
public class SandboxDataAuthoring : MonoBehaviour
{
	[SerializeField]
	private int defaultBrushSize;

	[SerializeField]
	private Sandbox.Controls controls;

	public class Baker : Baker<SandboxDataAuthoring>
	{
		public override void Bake(SandboxDataAuthoring authoring)
		{
			AddComponent(authoring.controls);
			AddComponent(new Sandbox.Painting.Matter());
			AddComponent(new Sandbox.Painting.Brush() { size = authoring.defaultBrushSize });
		}
	}
}

public static class Sandbox
{
	[Serializable]
	public struct Controls : IComponentData
	{
		public float cameraPanningSpeed;
		public float cameraMoveSpeed;
		public float cameraZoomSpeed;
		public float cameraZoomSmoothing;
		public float cameraMoveSpeedZoomCorrection;
	}

	public static class Painting
	{
		public struct Matter : IComponentData
		{
			public Entity matter;
		}

		public struct Brush : IComponentData
		{
			public int size;
		}
	}
}
