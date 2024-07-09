using FFXIVClientStructs.Havok.Common.Base.Container.Array;

namespace Ktisis.Common.Extensions;

public static class HavokEx {
	public static T[] Copy<T>(this hkArray<T> array) where T : unmanaged {
		var len = array.Length;
		var result = new T[len];
		for (var i = 0; i < len; i++)
			result[i] = array[i];
		return result;
	}

	public unsafe static void Initialize<T>(hkArray<T>* array, T* data = null, int length = 0) where T : unmanaged {
		array->Data = data;
		array->Length = length;
		*(uint*)&(array->CapacityAndFlags) = 0x80000000;
	}
}
