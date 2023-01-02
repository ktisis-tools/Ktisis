using System;
using System.Numerics;

namespace Ktisis.Library.Extensions {
	internal static class Numerics {
		// float

		internal static bool IsValid(this float f)
			=> !float.IsInfinity(f) && !float.IsNaN(f);

		internal static bool IsValid(this float? f)
			=> f != null && f.IsValid();

		// Vector3

		internal static Vector3 ClampMin(this Vector3 vec, float val) => new Vector3(
			Math.Max(vec.X, val),
			Math.Max(vec.Y, val),
			Math.Max(vec.Z, val)
		);

		// Vector4

		internal static uint ToRgba(this Vector4 vec)
			=> (uint)vec.W << 24 ^ (uint)vec.Z << 16 ^ (uint)vec.Y << 8 ^ (uint)vec.X;
	}
}