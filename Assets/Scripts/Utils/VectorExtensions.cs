using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class VectorExtension
{
	public static Vector2Int GetMultiplied(this Vector2Int a, Vector2Int b) =>
		new Vector2Int(a.x * b.x, a.y * b.y);

	public static Vector2Int GetDivided(this Vector2Int a, Vector2Int b) =>
		new Vector2Int(a.x / b.x, a.y / b.y);

	public static Vector2Int GetDivided(this Vector2Int a, int b) =>
		new Vector2Int(a.x / b, a.y / b);

	public static Vector2Int Round(this Vector2 vector) => new Vector2Int(
		Mathf.RoundToInt(vector.x), Mathf.RoundToInt(vector.y)
	);
	
	public static Vector2Int Ceil(this Vector2 vector) => new Vector2Int(
		Mathf.CeilToInt(vector.x), Mathf.CeilToInt(vector.y)
	);

	public static Vector2Int Floor(this Vector2 vector) => new Vector2Int(
		Mathf.FloorToInt(vector.x), Mathf.FloorToInt(vector.y)
	);

	public static bool IsWithin(this Vector2Int vec, Vector2Int from, Vector2Int to) =>
		vec.x >= from.x && vec.x <= to.x && vec.y >= from.y && vec.y <= to.y;

	public static int Area(this Vector2Int vec) => vec.x * vec.y;
}
