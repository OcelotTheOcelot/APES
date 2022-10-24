using System.Collections.Generic;
using System.Linq;
using Unity.Entities;
using UnityEngine;
using Verse;

public static class Utils
{
	/// <summary>
	/// Omits y components of two points and returns distance between them.
	/// </summary>
	/// <param name="source">Source point.</param>
	/// <param name="target">Destination point.</param>
	/// <returns>Horizontal distance.</returns>
	public static float HorizontalDistance(Vector3 source, Vector3 target)
	{
		target -= source;
		target.y = 0;
		return target.magnitude;
	}

	// Спиздил и немного рефакторнул.
	/// <summary>
	/// Generates a smoothed bezier curve from the provided line.
	/// </summary>
	/// <param name="line">The original line represented by an array of points.</param>
	/// <param name="smoothness">The smoothness of the line.
	/// Mostly represents the multiplication of the original line dots.
	/// Must be over 0.</param>
	/// <returns>Array of points the smoothed line consists of.</returns>
	public static Vector3[] SmoothLine(Vector3[] line, float smoothness)
	{
		List<Vector3> points;
		List<Vector3> smoothedLine;

		if (smoothness < 1f)
			throw new System.Exception("Smoothness parameter is not supposed to be less than 1.");

		int pointsLength = line.Length;

		int curvedLength = (pointsLength * Mathf.RoundToInt(smoothness)) - 1;
		smoothedLine = new List<Vector3>(curvedLength);

		for (int pointInTimeOnCurve = 0; pointInTimeOnCurve < curvedLength + 1; pointInTimeOnCurve++)
		{
			float t = Mathf.InverseLerp(0, curvedLength, pointInTimeOnCurve);

			points = new List<Vector3>(line);

			for (int j = pointsLength - 1; j > 0; j--)
				for (int i = 0; i < j; i++)
					points[i] = (1 - t) * points[i] + t * points[i + 1];

			smoothedLine.Add(points[0]);
		}

		return smoothedLine.ToArray();
	}

	/// <summary>
	/// Shortcut for quick picking a random item from a list.
	/// </summary>
	/// <typeparam name="T">The list's type.</typeparam>
	/// <param name="list">The list to pick from.</param>
	/// <returns>A random element from the list.</returns>
	public static T Pick<T>(List<T> list) => list[Random.Range(0, list.Count)];
	/// <summary>
	/// Shortcut for quick picking a random item from an array.
	/// </summary>
	/// <typeparam name="T">Array item type</typeparam>
	/// <param name="list">The array to pick from</param>
	/// <returns>A random element from the array</returns>
	public static T Pick<T>(T[] arr) => arr[Random.Range(0, arr.Length)];

	public static T Pick<T>(this DynamicBuffer<T> buffer) where T : unmanaged, IBufferElementData =>
		buffer[Apes.Random.FastRandom.GlobalInstance.Range(0, buffer.Length - 1)];

    public static T Pick<T>(this DynamicBuffer<T> buffer, int seed) where T : unmanaged, IBufferElementData =>
        buffer[AtomPhysics.Hash(seed) % buffer.Length];

    /// <summary>
    /// Searches for child transform of transfrom parent with given name.
    /// </summary>
    /// <param name="parent">Start point of the search.</param>
    /// <param name="name">The name of the game object to find.</param>
    public static Transform FindChildRecursively(Transform parent, string name)
	{
		if (parent.name.Equals(name))
			return parent;

		foreach (Transform child in parent)
			if (FindChildRecursively(child, name) is Transform result)
				return result;

		return null;
	}

	public static Vector3 ClampVector(Vector3 vec, Vector3 min, Vector3 max)
	{
		vec.x = Mathf.Clamp(vec.x, min.x, max.x);
		vec.y = Mathf.Clamp(vec.y, min.y, max.y);
		vec.z = Mathf.Clamp(vec.z, min.z, max.z);

		return vec;
	}

	public static float BellCurve(float x, float sigma = .3333f, float mu = 1f)
	{
		const float sqrt2pi = 2.5067f;

		float power = -Mathf.Pow(x - mu, 2) / (2 * Mathf.Pow(sigma, 2));

		return Mathf.Exp(power) / (sigma * sqrt2pi);
	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="x"></param>
	/// <param name="minX">Value of X, represents triple sigma offset from the maxX.</param>
	/// <param name="maxX">Value of X in which y takes the max value.</param>
	/// <returns>Corresponding y value scaled from 0 to 1.</returns>
	public static float BellCurveNormalized(float x, float minX, float maxX)
	{
		float sigma = Mathf.Abs(maxX - minX) / 3f;
		float mu = maxX;
		return BellCurve(x, sigma: sigma, mu: mu) * 2.5f;
	}
}