using Apes.Random;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities.UniversalDelegates;
using UnityEngine;

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

	public static IEnumerable<Vector2Int> GetSquare(int size)
	{
		for (int y = 0; y < size; y++)
			for (int x = 0; x < size; x++)
				yield return new Vector2Int(x, y);
	}

	public static IEnumerable<Vector2Int> GetRect(Vector2Int from, Vector2Int to, Order order = Order.RightUp)
	{
		switch (order)
		{
		case Order.RightUp:
			for (int y = from.y; y < to.y; y++)
				for (int x = from.x; x < to.x; x++)
					yield return new Vector2Int(x, y);
			break;

		case Order.UpRight:
			for (int x = from.x; x < to.x; x++)
				for (int y = from.y; y < to.y; y++)
					yield return new Vector2Int(x, y);
			break;

		case Order.LeftDown:
			for (int y = to.y - 1; y >= from.y; y--)
				for (int x = to.x - 1; x >= from.x; x--)
					yield return new Vector2Int(x, y);
			break;
		}
	}
	public static IEnumerable<Vector2Int> GetRect(Vector2Int to, Order order = Order.RightUp) => GetRect(Vector2Int.zero, to, order);

	/// <summary>
	/// 7 8 9
	/// 6 5 4
	/// 1 2 3
	/// </summary>
	/// <param name="from"></param>
	/// <param name="to"></param>
	/// <returns></returns>
	public static IEnumerable<Vector2Int> GetSnakeWithTickOddity(Vector2Int from, Vector2Int to, int tick)
	{
		int oddity = (tick + from.y) & 1;

		for (int y = from.y; y <= to.y; y++)
		{
			if (oddity == 1)
				for (int x = from.x; x <= to.x; x++)
					yield return new Vector2Int(x, y);
			else
				for (int x = to.x - 1; x >= from.x; x--)
					yield return new Vector2Int(x, y);

			oddity ^= 1;
		}
	}

	public static IEnumerable<int> GetFlatTickSnake(int from, int to, int snakeWidth, int tick)
	{
		throw new NotImplementedException();
	}


	public static IEnumerable<Vector2Int> GetCircle(int radius, Vector2Int center)
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
				yield return center + new Vector2Int(x, y);
		}
	}

	/// <summary>
	/// 3 4 3
	/// 2 x 2
	/// 1 0 1
	/// </summary>
	/// <param name="swing"></param>
	/// <returns></returns>
	public static IEnumerable<Vector2Int> GetPendulum(Vector2Int center, int swing = 4)
	{
		yield return center + Vector2Int.down;

		int maxSideSwing = -2 + Mathf.Max(swing, 3);
		
		int direction;
		for (int i = -1; i <= maxSideSwing; i++)
		{
			direction = FastRandom.GlobalInstance.GetFloat() > .5f ? 1 : -1;
			yield return center + new Vector2Int(direction, i);
			yield return center + new Vector2Int(-direction, i);
		}

		if (swing >= 4)
			yield return center + Vector2Int.up;
	}


	/// <summary>
	/// Basically, a call to GetPendulum(2) – it should be more efficient without any conditioning.
	/// </summary>
	/// <returns></returns>
	public static IEnumerable<Vector2Int> GetHalfPendulum(Vector2Int center, int tick)
	{
		yield return new Vector2Int(center.x, center.y - 1);

		int direction = ((tick & 1) << 1) - 1;
		
		yield return new Vector2Int(center.x + direction, center.y - 1);
		yield return new Vector2Int(center.x - direction, center.y - 1);
		yield return new Vector2Int(center.x + direction, center.y);
		yield return new Vector2Int(center.x - direction, center.y);
	}

	public readonly static Vector2Int[] halfPendulumRight = new Vector2Int[5]
	{
			new Vector2Int(0, -1),
			new Vector2Int(1, -1),
			new Vector2Int(-1, -1),
			new Vector2Int(1, 0),
			new Vector2Int(-1, 0)
	};

	public readonly static Vector2Int[] halfPendulumLeft = new Vector2Int[5]
	{
			new Vector2Int(0, -1),
			new Vector2Int(-1, -1),
			new Vector2Int(1, -1),
			new Vector2Int(-1, 0),
			new Vector2Int(1, 0)
	};

	/// <summary>
	/// x 1 x
	/// 2 x 0
	/// x 3 x
	/// </summary>
	/// <returns></returns>
	public static IEnumerable<Vector2Int> GetCross(Vector2Int center)
	{
		yield return center + Vector2Int.right;
		yield return center + Vector2Int.up;
		yield return center + Vector2Int.left;
		yield return center + Vector2Int.down;
	}

	/// <summary>
	/// 3 2 1
	/// 4 x 0
	/// 5 6 7
	/// </summary>
	/// <returns></returns>
	public static IEnumerable<Vector2Int> GetARound(Vector2Int center)
	{
		yield return center + Vector2Int.right;
		yield return center + Vector2Int.one;
		yield return center + Vector2Int.up;
		yield return center + new Vector2Int(-1, 1);
		yield return center + Vector2Int.left;
		yield return center - Vector2Int.one;
		yield return center + Vector2Int.down;
		yield return center + new Vector2Int(1, -1);
	}

	/// <summary>
	/// 2 3
	/// 0 1
	/// </summary>
	/// <param name="rect"></param>
	/// <returns></returns>
	public static IEnumerable<Vector2Int> GetCorners(RectInt rect)
	{
		yield return rect.min;
		yield return new Vector2Int(rect.xMax, rect.yMin);
		yield return new Vector2Int(rect.xMin, rect.yMax);
		yield return rect.max;
	}
}
