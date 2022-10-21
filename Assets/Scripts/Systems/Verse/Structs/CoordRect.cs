using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Verse
{
	public struct CoordRect
	{
		public Coord min;
		public Coord max;

		public CoordRect(Coord min, Coord max)
		{
			this.min = min;
			this.max = max;
		}
		public static CoordRect CreateRectBetween(Coord a, Coord b, int margin = 0)
		{
			Coord min = a;
			Coord max = b;

			if (a.x > b.x)
			{
				min.x = b.x;
				max.x = a.x;
			}

			if (a.y > b.y)
			{
				min.y = b.y;
				max.y = a.y;
			}

			min.x -= margin;
			min.y -= margin;
			max.x += margin;
			max.y += margin;

			return new CoordRect(min, max);
		}

		public CoordRect(int min, int max) : this()
		{
			xMin = min;
			xMax = max;
			yMin = min;
			yMax = max;
		}

		public CoordRect(int xMin, int yMin, int xMax, int yMax) : this()
		{
			this.xMin = xMin;
			this.xMax = xMax;
			this.yMin = yMin;
			this.yMax = yMax;
		}

		public int xMin
		{
			get => min.x;
			set => min.x = value;
		}
		public int yMin
		{
			get => min.y;
			set => min.y = value;
		}
		public int xMax
		{
			get => max.x;
			set => max.x = value;
		}
		public int yMax
		{
			get => max.y;
			set => max.y = value;
		}

		public int Width => xMax - xMin;
		public Coord Size => max - min;
		public int Area => (max - min).Product;

		public void Set(Coord min, Coord max)
		{
			this.min = min;
			this.max = max;
		}

		public static CoordRect operator +(CoordRect rect, Coord shift) => new(rect.min + shift, rect.max + shift);
		public static CoordRect operator -(CoordRect rect, Coord shift) => new(rect.min - shift, rect.max - shift);

		public CoordRect GetShifted(int shiftX, int shiftY) => new(xMin + shiftX, yMin + shiftY, xMax + shiftX, yMax + shiftY);

		public void StretchCombineWith(CoordRect otherRect)
		{
			min = Coord.Min(min, otherRect.min);
			max = Coord.Max(max, otherRect.max);
		}

		public bool IntersectWith(CoordRect otherRect)
		{
			if (xMax < otherRect.xMin || xMin > otherRect.xMax || yMin > otherRect.yMax || yMax < otherRect.yMin)
				return false;

			IntersectWithNonSafe(otherRect);
			return true;
		}

		public bool Contains(Coord coord)
		{
			if (xMax < coord.x || xMin > coord.x || yMin > coord.y || yMax < coord.y)
				return false;

			return true;
		}

		public void IntersectWithNonSafe(CoordRect otheRect)
		{
			Set(
				Coord.Max(min, otheRect.min),
				Coord.Min(max, otheRect.max)
			);
		}

		public override string ToString() => $"[{min}–{max}]";
	}
}