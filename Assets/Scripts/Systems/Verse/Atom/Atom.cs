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

        public struct Dynamics : IComponentData
        {
            public float2 velocity;
            public float2 acceleration;

			public Dynamics(float2 velocity) : this()
			{
				this.velocity = velocity;
				acceleration = float2.zero;

            }
		}
    }
}
