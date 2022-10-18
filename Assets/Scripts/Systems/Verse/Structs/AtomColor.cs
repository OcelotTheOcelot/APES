using System.Runtime.InteropServices;
using UnityEngine;

namespace Verse
{
	[StructLayout(LayoutKind.Explicit)]
	public struct AtomColor
	{
		[FieldOffset(0)]
		public int rgba;

		[FieldOffset(0)]
		public byte r;

		[FieldOffset(1)]
		public byte g;

		[FieldOffset(2)]
		public byte b;

		[FieldOffset(3)]
		public byte a;

		public AtomColor(byte red, byte green, byte blue, byte alpha)
		{
			rgba = 0;

			r = red;
			g = green;
			b = blue;
			a = alpha;
		}

		public static implicit operator Color32(AtomColor pixel) => new(pixel.r, pixel.g, pixel.b, pixel.a);
		public static implicit operator AtomColor(Color32 color) => new(color.r, color.g, color.b, color.a);
		public static implicit operator AtomColor(Color color) => new(
			(byte)(color.r * 255f),
			(byte)(color.g * 255f),
			(byte)(color.b * 255f),
			(byte)(color.a * 255f)
		);
	}
}