using Unity.Entities.UniversalDelegates;
using Unity.Mathematics;
using UnityEngine;

namespace Verse
{
	public static class ParticlePhysics
	{
		public static readonly float maxVelocity = 32f;
		public static readonly float gravity = 9.8f;
		public static readonly float perTickGravity = -gravity / 60f;

		public static readonly float halfSqrt2 = .70710678118f;
		public static readonly float2 velE = new(1f, 0f);
		public static readonly float2 velNE = new(halfSqrt2, halfSqrt2);
		public static readonly float2 velN = new(0f, 1f);
		public static readonly float2 velNW = new(-halfSqrt2, halfSqrt2);
		public static readonly float2 velW = new(-1f, 0f);
		public static readonly float2 velSW = new(-halfSqrt2, -halfSqrt2);
		public static readonly float2 velS = new(0f, -1f);
		public static readonly float2 velSE = new(halfSqrt2, -halfSqrt2);

		// Reflects vector against a surface with normal pointing to NE
		public static float2 ReflectAgainstNE(float2 v) => new(-v.y, -v.x);
		public static float2 ReflectAgainstNW(float2 v) => new(v.y, v.x);
		public static float2 ReflectAgainstSE(float2 v) => new(v.y, v.x);
		public static float2 ReflectAgainstSW(float2 v) => new(-v.y, -v.x);
		public static float2 ReflectAgainst45(float2 v, int normalX, int normalY) =>
			(normalX * normalY >= 0) ? new(-v.y, -v.x) : new(v.y, v.x);
	}
}