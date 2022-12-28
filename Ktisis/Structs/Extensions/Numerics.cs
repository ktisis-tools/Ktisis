using Ktisis.Library;

using System;
using System.Numerics;

namespace Ktisis.Structs.Extensions {
	public static class Numerics {
		// float

		public static bool IsValid(this float f)
			=> !float.IsInfinity(f) && !float.IsNaN(f);

		public static bool IsValid(this float? f)
			=> f != null && f.IsValid();

		// Vector3

		public static Vector3 ClampMin(this Vector3 vec, float val) => new Vector3(
			Math.Max(vec.X, val),
			Math.Max(vec.Y, val),
			Math.Max(vec.Z, val)
		);

		// Vector4

		public static uint ToRgba(this Vector4 vec) {
			vec *= 255;
			return (uint)vec.W << 24 ^ (uint)vec.Z << 16 ^ (uint)vec.Y << 8 ^ (uint)vec.X;
		}
	}
}