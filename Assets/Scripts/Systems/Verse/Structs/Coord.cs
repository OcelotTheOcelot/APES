using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Mathematics;
using UnityEngine;

namespace Verse
{

	[StructLayout(LayoutKind.Explicit)]
	public struct Coord	
	{
		[FieldOffset(0)]
		public int2 xy;

		public int x
		{
			get => xy.x;
			set => xy.x = value; 
		}

		public int y
		{
			get => xy.y;
			set => xy.y = value;
		}

		public int Product => x * y;
		public int Sum => x + y;

		public Coord(Vector2Int coord) : this(coord.x, coord.y) { }
		public Coord(int2 xy) { this.xy = xy; }
		public Coord(int x, int y) : this() { xy = new(x, y); }
		public Coord(int value) : this() { xy = new(value); }

		public static Coord Round(float2 vec) => new(Mathf.RoundToInt(vec.x), Mathf.RoundToInt(vec.y));
		public static Coord Ceil(float2 vec) => new(Mathf.CeilToInt(vec.x), Mathf.CeilToInt(vec.y));
		public static Coord Floor(float2 vec) => new(Mathf.FloorToInt(vec.x), Mathf.FloorToInt(vec.y));

		public void Set(int x, int y)
		{
			this.x = x;
			this.y = y;
		}

		public readonly static Coord zero = new(0, 0);
		public readonly static Coord one = new(1, 1);

		public readonly static Coord east = new(1, 0);
		public readonly static Coord northEast = new(1, 1);
		public readonly static Coord north = new(0, 1);
		public readonly static Coord northWest = new(-1, 1);
		public readonly static Coord west = new(-1, 0);
		public readonly static Coord southWest = new(-1, -1);
		public readonly static Coord south = new(0, -1);
		public readonly static Coord southEast = new(1, -1);

		public void Clamp(Coord min, Coord max)
		{
			if (x < min.x)
				x = min.x;
			else if (x > max.x)
				x = max.x;

			if (y < min.y)
				y = min.y;
			else if (x > max.y)
				y = max.y;
		}

        public Coord GetShifted(int shiftX, int shiftY) => new(this.x + shiftX, y + shiftY);

        public static Coord Min(Coord a, Coord b) => new(math.min(a.x, b.x), math.min(a.y, b.y));
		public static Coord Max(Coord a, Coord b) => new(math.max(a.x, b.x), math.max(a.y, b.y));

		public static implicit operator Coord(Vector2Int coord) => new(coord);
		public static implicit operator Coord(int2 coord) => new(coord);
		public static implicit operator int2(Coord coord) => coord.xy;
		public static implicit operator float2(Coord coord) => coord.xy;
		public static implicit operator Vector2(Coord coord) => new(coord.x, coord.y);

		public static Coord operator +(Coord a, Coord b) => new(a.xy + b.xy);
		public static Coord operator +(Coord a, int2 b) => new(a.xy + b);
		public static Coord operator -(Coord a, Coord b) => new(a.xy - b.xy);
		public static Coord operator -(Coord a, int2 b) => new(a.xy - b);

		public static Coord operator *(Coord a, Coord b) => new(a.xy * b.xy);
		public static Coord operator *(Coord a, int b) => new(a.xy * b);
		public static float2 operator *(Coord a, float b) => new(a.x * b, a.y * b);

		public static Coord operator /(Coord a, Coord b) => new(a.xy / b.xy);
		public static Coord operator /(Coord a, int b) => new(a.xy / b);
		public static float2 operator /(Coord a, float b) => new(a.x / b, a.y / b);

		public static bool operator ==(Coord a, Coord b) => a.xy.Equals(b.xy);
		public static bool operator !=(Coord a, Coord b) => a.x != b.x || a.y != b.y;

		public override string ToString() => $"({x}, {y})";

		public override bool Equals(object other)
		{
			if (other is Coord otherCoord)
				return xy.Equals(otherCoord.xy);
			return false;
		}

		public override int GetHashCode() => xy.GetHashCode();
	}
}