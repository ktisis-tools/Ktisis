using FFXIVClientStructs.Havok;

namespace Ktisis.Common.Extensions;

public static class HavokEx {
	public static T[] Copy<T>(this hkArray<T> array) where T : unmanaged {
		var len = array.Length;
		var result = new T[len];
		for (var i = 0; i < len; i++)
			result[i] = array[i];
		return result;
	}
}