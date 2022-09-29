using UnityEngine;
using Unity.Entities;

public static class RectExtension
{
	/// <summary>
	/// Creates a rectangle between the two given points
	/// There's no MinMaxRect method for RectInt
	/// </summary>
	/// <param name="a"></param>
	/// <param name="b"></param>
	/// <param name="margin"></param>
	/// <returns></returns>
	public static RectInt CreateRectBetween(Vector2Int a, Vector2Int b, int margin = 0, int additiveSize = 0)
	{
		int xMin, yMin, width, height;

		// This should be more effective than sorting or using Min and Max functions twice.
		// I guess. I'm not good with computers.
		if (a.x < b.x)
			width = b.x - (xMin = a.x);
		else
			width = a.x - (xMin = b.x);

		if (a.y < b.y)
			height = b.y - (yMin = a.y);
		else
			height = a.y - (yMin = b.y);

		return new RectInt(
			xMin - margin,
			yMin - margin,
			width + (margin << 1) + additiveSize,
			height + (margin << 1) + additiveSize
		);
	}

	public static bool IntersectWith(ref this RectInt a, RectInt b)
	{
		if (a.xMax < b.xMin || a.xMin > b.xMax || a.yMin > b.yMax || a.yMax < b.yMin)
			return false;

		IntersectWithNonSafe(ref a, b);
		return true;
	}

	public static void IntersectWithNonSafe(ref this RectInt a, RectInt b)
	{
		a.SetMinMax(
			Vector2Int.Max(a.min, b.min),
			Vector2Int.Min(a.max, b.max)
		);
	}

	public static RectInt GetInflated(this RectInt rect, int inflation)
	{
		int doubleInflation = inflation << 1;
		return new RectInt(
			rect.xMin - inflation, rect.yMin - inflation,
			rect.width + doubleInflation, rect.height + doubleInflation
		);
	}

	public static RectInt GetInflated(this RectInt rect) => new (rect.xMin - 1, rect.yMin - 1, rect.width + 2, rect.height + 2);
}