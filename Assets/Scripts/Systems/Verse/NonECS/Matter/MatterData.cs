using System;
using System.Collections;
using System.Collections.Generic;
using TypeReferences;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Verse
{
	// [CreateAssetMenu(fileName = "MatterData", menuName = "APEZ/Matter", order = 1)]
	public class MatterData : MonoBehaviour, IConvertGameObjectToEntity
	{
		// Must be unique
		[SerializeField]
		private string id = "unknown";
		
		[SerializeField]
		private string group = "unknown group";

		// Visuals

		[SerializeField]
		private string displayName = "unknown matter";

		[SerializeField]
		private Color32[] colors = new Color32[0];

		// Physics

		[SerializeField]
		private MatterState state = MatterState.Solid;

		// KG per cubic meter (it's funny to say "cubic" in a 2D game, but whatever)
		[SerializeField]
		private float density = 1000f;

		// Measured in Celsium
		public float defaultTemperature = 20f;

		public static readonly float AbsoluteZero = -273.15f;

		public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
		{
			dstManager.AddComponentData(entity, new Id { id = id });
			dstManager.AddComponentData(entity, new Group { group = group });
			dstManager.AddComponentData(entity, new DisplayName { name = displayName });

			dstManager.AddComponentData(entity, new State { state = state });
			dstManager.AddComponentData(entity, new Creation { temperature = defaultTemperature });

			dstManager.AddComponentData(entity, new PhysicProperties { density = density });
			
			var buffer = dstManager.AddBuffer<ColorBufferElement>(entity);
			foreach (Color color in colors)
				buffer.Add(color);

			MatterLibrary.Add(id, entity);
		}

		public struct Id : IComponentData
		{
			public FixedString32Bytes id;
		}

		public struct ColorBufferElement : IBufferElementData
		{
			public Color color;

			public static implicit operator Color(ColorBufferElement matterColor) => matterColor.color;
			public static implicit operator ColorBufferElement(Color color) => new() { color = color };

			public static implicit operator AtomData.Color(ColorBufferElement matterColor) => new AtomData.Color { color = matterColor.color };
		}

		public struct State : IComponentData
		{
			public MatterState state;
		}

		public struct Creation : IComponentData
		{
			public float temperature;
		}

		public struct Group : IComponentData
		{
			public FixedString32Bytes group;
		}

		public struct DisplayName : IComponentData
		{
			public FixedString32Bytes name;
		}

		public struct PhysicProperties : IComponentData
		{
			public float density;
		}
	}

	public enum MatterState
	{
		BoseEinstein,
		Solid,
		Liquid,
		Gaseous,
		Plasma,
		Beyond
	}
}