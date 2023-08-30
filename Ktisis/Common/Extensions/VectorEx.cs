using System.Numerics;

namespace Ktisis.Common.Extensions;

public static class VectorEx {
	public static Vector2 Add(this Vector2 vec, float num)
		=> new Vector2(vec.X + num, vec.Y + num);

	public static Vector2 AddX(this Vector2 vec, float num)
		=> new Vector2(vec.X + num, vec.Y);

	public static Vector2 AddY(this Vector2 vec, float num)
		=> new Vector2(vec.X, vec.Y + num);
}
