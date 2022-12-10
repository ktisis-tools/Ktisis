namespace Ktisis.Structs.Extensions {
	public static class Numerics {
		public static bool IsValid(this float f)
			=> !float.IsInfinity(f) && !float.IsNaN(f);

		public static bool IsValid(this float? f)
			=> f != null && f.IsValid();
	}
}
