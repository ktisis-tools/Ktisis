using System.Linq;

namespace Ktisis.Library.Extensions {
	internal static class Enum {
		internal static T AddFlag<T>(this System.Enum type, T value) => (T)(object)((int)(object)type | (int)(object)value!);
		internal static T RemoveFlag<T>(this System.Enum type, T value) => (T)(object)((int)(object)type & ~(int)(object)value!);
		internal static T ToggleFlag<T>(this System.Enum type, T value) => (T)(object)((int)(object)type ^ (int)(object)value!);
		internal static T ToggleFlag<T>(this System.Enum type, params T[] values) {
			var temp = (int)(object)type;
			temp = values.Aggregate(temp, (current, value) => current ^ (int)(object)value!);
			return (T)(object)temp;
		}
	}
}