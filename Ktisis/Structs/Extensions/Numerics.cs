namespace Ktisis.Structs.Extensions {
	public static class Numerics {
		public static bool IsValid(this float? f)
			=> f != null && !float.IsInfinity((float)f) && !float.IsNaN((float)f);
	}
}