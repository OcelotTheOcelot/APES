using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class WaveNoise
{
	public static float Noise(float x, float a=2) =>
		Mathf.Sin(a * x) + Mathf.Sin(Mathf.PI * x);

	public static float Hills(float x, float width, float height, float a=2, float shift=0) =>
		Noise(x / width, a) * height;
}
