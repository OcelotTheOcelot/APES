using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Verse
{
	public static class AtomPhysics
	{
		public static readonly float motionPerTick = 1f;

		public static readonly float maxSpeed = 32f;

		public static readonly float gravity = 9.8f;
		public static readonly float perTickGravity = -gravity / 60f;
		public static readonly float2 perTickGravityAcceleration = new(0f, -perTickGravity);

		public static readonly float halfSqrt2 = .70710678118f;
		public static readonly float2 velE = new(1f, 0f);
		public static readonly float2 velNE = new(halfSqrt2, halfSqrt2);
		public static readonly float2 velN = new(0f, 1f);
		public static readonly float2 velNW = new(-halfSqrt2, halfSqrt2);
		public static readonly float2 velW = new(-1f, 0f);
		public static readonly float2 velSW = new(-halfSqrt2, -halfSqrt2);
		public static readonly float2 velS = new(0f, -1f);
		public static readonly float2 velSE = new(halfSqrt2, -halfSqrt2);

		public static readonly float speedUpdateTreshold = .1f;

		// Reflects vector against a surface with normal pointing to NE
		public static float2 ReflectAgainstNE(float2 v) => new(-v.y, -v.x);
		public static float2 ReflectAgainstNW(float2 v) => new(v.y, v.x);
		public static float2 ReflectAgainstSE(float2 v) => new(v.y, v.x);
		public static float2 ReflectAgainstSW(float2 v) => new(-v.y, -v.x);
		public static float2 ReflectAgainst45(float2 v, int normalX, int normalY) =>
			(normalX * normalY >= 0) ? new(-v.y, -v.x) : new(v.y, v.x);

		public static int2 RoundToCoord(float2 v) => math.int2(math.round(math.abs(v)) * math.sign(v));

		public readonly static float2 maxDisplacement = new(maxSpeed - 1);
		public readonly static float2 minDisplacement = new(-(maxSpeed - 1));

        public static int Hash(int key)
		{
			key = (key + 0x7ed55d16) + (key << 12);
			key = (key ^ 0x5761C23C) ^ (key >> 19);
			key = (key + 0x165667b1) + (key << 5);
			key = (key + 0x33a2646c) ^ (key << 9);
			key = (key + 0x4d7046c5) + (key << 3);
			key = (key ^ 0x155a4f09) ^ (key >> 16);
			return key;
		}

        public static uint Hash(uint key)
        {
            key = (key + 0x7ed55d16) + (key << 12);
            key = (key ^ 0x8761C23C) ^ (key >> 19);
            key = (key + 0x165667b1) + (key << 5);
            key = (key + 0xd3a2646c) ^ (key << 9);
            key = (key + 0xfd7046c5) + (key << 3);
            key = (key ^ 0xb55a4f09) ^ (key >> 16);
            return key;
        }

		public static float FourierOneBy(float denominator)
		{
			throw new System.NotImplementedException();
			const int i = 3;
			const float omega = 0f;
            return -i * 1.25331413732f * math.sign(omega);
        }

        //public static int Hash(int value)
        //{
        //	value = (value ^ 61) ^ (value >> 16);
        //	value += (value << 3);
        //	value ^= (value >> 4);
        //	value *= 0x27d4eb2d;
        //	value ^= (value >> 15);
        //	return value;
        //}

        public static void PerfectlyInelasticCollision(ref float2 thisVel, ref float2 otherVel) =>
			thisVel = otherVel = (thisVel + otherVel) * .5f;

		public static void PerfectlyInelasticCollision(ref float2 thisVel, float thisMass, ref float2 otherVel, float otherMass)
		{
			thisVel = otherVel = (thisVel * thisMass + otherVel * otherMass) / (thisMass + otherMass);
		}

		public static void PerfectlyElasticCollision(ref float2 thisVel, ref float2 otherVel)
		{
			thisVel = otherVel;
			otherVel = thisVel;
		}

		public static void PerfectlyElasticCollision(ref float2 thisVel, float thisMass, ref float2 otherVel, float otherMass)
		{
			thisVel = otherVel;
			otherVel = thisVel;
		}

		public static void Collision(
			ref float2 thisVel, Matter.PhysicProperties thisProps,
			ref float2 otherVel, Matter.PhysicProperties otherProps
		) => Collision(ref thisVel, thisProps.density, thisProps.elasticity, ref otherVel, otherProps.density, otherProps.elasticity);

		public static void Collision(
			ref float2 thisVel, float thisMass, float thisElasticity,
			ref float2 otherVel, float otherMass, float otherElasticity
		)
		{
			throw new System.NotImplementedException();
		}

		public static void PassThrough(
			ref float2 thisVel, Matter.PhysicProperties thisProps,
			ref float2 otherVel, Matter.PhysicProperties otherProps
		) => PassThrough(ref thisVel, thisProps.density, thisProps.friction, ref otherVel, otherProps.density, otherProps.friction);

		public static void PassThrough(
			ref float2 thisVel, float thisMass, float thisFriction,
			ref float2 otherVel, float otherMass, float otherFriction
		)
		{
			throw new System.NotImplementedException();
		}

		public static void Collision(
			ref float2 thisVel, Matter.PhysicProperties thisProps,
			float2 otherVel, Matter.PhysicProperties otherProps
		)
		{
			throw new System.NotImplementedException();
		}
	}
}