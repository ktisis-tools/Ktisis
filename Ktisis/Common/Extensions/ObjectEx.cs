namespace Ktisis.Common.Extensions;

public static class ObjectEx {
	public static string GenerateId(this object obj)
		=> $"{obj.GetType().Name}#{obj.GetHashCode():X}";
}
