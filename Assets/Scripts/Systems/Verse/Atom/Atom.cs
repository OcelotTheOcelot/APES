using UnityEngine;
using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using System;

namespace Verse
{
	public static class Atom
	{
		public struct Matter : IComponentData
		{
			[ReadOnly]
			public Entity value;

			public Matter(Entity value) { this.value = value; }
		}

		public struct Color : IComponentData
		{
			public AtomColor value;
			public Color(AtomColor value) { this.value = value; }
			public static implicit operator AtomColor(Color color) => color.value;
		}

		public struct Temperature : IComponentData
		{
			public float value;
			public Temperature(float value) { this.value = value; }
		}

        public struct Velocity : IComponentData
        {
            public float2 value;

            public Velocity(float2 value) { this.value = value; }

            public static implicit operator Velocity(float2 value) => new(value);
            public static implicit operator float2(Velocity velocity) => velocity.value;
        }
    }
}
