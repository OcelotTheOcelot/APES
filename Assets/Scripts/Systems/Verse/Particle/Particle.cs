using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Verse
{
	public static class Particle
	{
        public struct OriginalAtom : IComponentData
		{
			public Entity value;
		}

        public struct Position : IComponentData
		{
			public float2 value;

			public Position(float2 value) { this.value = value; }

			public static implicit operator Position(float2 value) => new(value);
			public static implicit operator float2(Position position) => position.value;
			public static implicit operator Coord(Position position) => new(math.int2(position.value));
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