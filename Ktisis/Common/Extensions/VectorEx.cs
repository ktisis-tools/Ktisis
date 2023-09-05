using System.Numerics;

namespace Ktisis.Common.Extensions; 

public static class VectorEx {
	public static Vector2 Add(this Vector2 vec, float x, float y)
		=> new Vector2(vec.X + x, vec.Y + y);
	
	public static Vector2 Add(this Vector2 vec, float num)
		=> new Vector2(vec.X + num, vec.Y + num);

	public static Vector2 AddX(this Vector2 vec, float num)
		=> new Vector2(vec.X + num, vec.Y);

	public static Vector2 AddY(this Vector2 vec, float num)
		=> new Vector2(vec.X, vec.Y + num);
	
	public static Vector2 Sub(this Vector2 vec, float x, float y)
		=> new Vector2(vec.X - x, vec.Y - y);
	
	public static Vector2 Sub(this Vector2 vec, float num)
		=> new Vector2(vec.X - num, vec.Y - num);

	public static Vector2 SubX(this Vector2 vec, float num)
		=> new Vector2(vec.X - num, vec.Y);

	public static Vector2 SubY(this Vector2 vec, float num)
		=> new Vector2(vec.X, vec.Y - num);
}
