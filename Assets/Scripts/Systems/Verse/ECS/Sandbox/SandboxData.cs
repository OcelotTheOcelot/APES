using System;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Verse;

[DisallowMultipleComponent]
public class SandboxData : MonoBehaviour, IConvertGameObjectToEntity
{
	[SerializeField]
	private int defaultBrushSize;

	[SerializeField]
	private Controls controls;

	public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
	{
		dstManager.AddComponentData(entity, controls);
		dstManager.AddComponentData(entity, new PaintingMatter());
		dstManager.AddComponentData(entity, new Brush());
	}

	[Serializable]
	public struct Controls : IComponentData
	{
		public float cameraPanningSpeed;
		public float cameraMoveSpeed;
		public float cameraZoomSpeed;
		public float cameraZoomSmoothing;
		public float cameraMoveSpeedZoomCorrection;
	}

	public struct PaintingMatter : IComponentData
	{
		public Entity matter;
	}

	public struct Brush : IComponentData
	{
		public int size;
	}
}
