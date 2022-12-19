using System.Numerics;

namespace Ktisis.Structs.Extensions {
	public static class Numerics {
		public static bool IsValid(this float f)
			=> !float.IsInfinity(f) && !float.IsNaN(f);

		public static bool IsValid(this float? f)
			=> f != null && f.IsValid();

		public static uint ToRgba(this Vector4 vec) {
			vec *= 255;
			return (uint)vec.W << 24 ^ (uint)vec.Z << 16 ^ (uint)vec.Y << 8 ^ (uint)vec.X;
		}
	}
}