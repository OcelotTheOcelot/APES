using Apes.Random;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities.UniversalDelegates;
using UnityEngine;
using Verse;

public static class Enumerators
{
	public enum Order
	{
		// Assume right as Unity's positive X and up as Unity's positive Y

		RightUp,
		UpRight,

		// Like one reads a book in european languages
		RightDown,

		// Like in arabic or hebrew writings
		LeftDown,

		// Like in Japanese or Chinese
		DownRight,

		// Like in Mongolian
		DownLeft,

		// Hilbert's spiral, starts from right, then goes counter-clockwise
		Hilbert
	}

	public static IEnumerable<Coord> GetSquare(int size)
	{
		for (int y = 0; y < size; y++)
			for (int x = 0; x < size; x++)
				yield return new Coord(x, y);
	}

	public static IEnumerable<Coord> GetRect(Coord from, Coord to, Order order = Order.RightUp)
	{
		switch (order)
		{
		case Order.RightUp:
			for (int y = from.y; y < to.y; y++)
				for (int x = from.x; x < to.x; x++)
					yield return new Coord(x, y);
			break;

		case Order.UpRight:
			for (int x = from.x; x < to.x; x++)
				for (int y = from.y; y < to.y; y++)
					yield return new Coord(x, y);
			break;

		case Order.LeftDown:
			for (int y = to.y - 1; y >= from.y; y--)
				for (int x = to.x - 1; x >= from.x; x--)
					yield return new Coord(x, y);
			break;
		}
	}
	public static IEnumerable<Coord> GetRect(Coord to, Order order = Order.RightUp) => GetRect(Coord.zero, to, order);

	public static IEnumerable<Coord> GetCircle(int radius, Coord center)
	{
		if (radius == 0)
		{
			yield return center;
			yield break;
		}

		for (int x = -radius; x <= radius; x++)
		{
			int height = Mathf.FloorToInt(Mathf.Sqrt(radius * radius - x * x));
			for (int y = -height; y <= height; y++)
				yield return center + new Coord(x, y);
		}
	}

	public readonly static Coord[] halfPendulumRight = new Coord[5] { Coord.south, Coord.southEast, Coord.southWest, Coord.east, Coord.west };
	public readonly static Coord[] halfPendulumLeft = new Coord[5] { Coord.south, Coord.southWest, Coord.southEast, Coord.west, Coord.east };

	public readonly static Coord[] lowerPendulumRight = new Coord[3] { Coord.south, Coord.southEast, Coord.southWest };
	public readonly static Coord[] lowerPendulumLeft = new Coord[3] { Coord.south, Coord.southWest, Coord.southEast };

	/// <summary>
	/// x 1 x
	/// 2 x 0
	/// x 3 x
	/// </summary>
	/// <returns></returns>
	public static IEnumerable<Coord> GetCross(Coord center)
	{
		yield return center + Coord.east;
		yield return center + Coord.north;
		yield return center + Coord.west;
		yield return center + Coord.south;
	}

	/// <summary>
	/// 3 2 1
	/// 4 x 0
	/// 5 6 7
	/// </summary>
	/// <returns></returns>
	public static IEnumerable<Coord> GetARound(Coord center)
	{
		yield return center + Coord.east;
		yield return center + Coord.northEast;
		yield return center + Coord.north;
		yield return center + Coord.northWest;
		yield return center + Coord.west;
		yield return center + Coord.southWest;
		yield return center + Coord.south;
		yield return center + Coord.southEast;
	}
}
