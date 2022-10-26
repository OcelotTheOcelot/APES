using UnityEngine;
using Unity.Entities;
using Unity.Collections;

namespace Verse
{
	public static class Matter
	{
		public static PhysicProperties GetPhysicalProperties(EntityManager dstManager, Entity matter) =>
			dstManager.GetComponentData<PhysicProperties>(matter);

		public static State GetState(EntityManager dstManager, Entity matter) =>
			dstManager.GetComponentData<AtomState>(matter).value;


		public struct Id : IComponentData
		{
			public FixedString32Bytes id;
		}

		public struct ColorBufferElement : IBufferElementData
		{
			public Color color;

			public static implicit operator Color(ColorBufferElement matterColor) => matterColor.color;
			public static implicit operator ColorBufferElement(Color color) => new() { color = color };

			public static implicit operator Atom.Color(ColorBufferElement matterColor) => new() { value = matterColor.color };
		}

		public struct AtomState : IComponentData
		{
			public State value;
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
			public float friction;
			public float elasticity;
            //public float viscosity;
            //public float adhesiveness;
        }

        public enum State
		{
			Solid,
			Powder,
            Liquid,
			Gaseous,
			Plasma
		}
	}
}
