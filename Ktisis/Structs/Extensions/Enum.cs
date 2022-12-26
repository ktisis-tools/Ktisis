using System.Linq;

namespace Ktisis.Structs.Extensions {
	internal static class Enum {
		public static T AddFlag<T>(this System.Enum type, T value) => (T)(object)((int)(object)type | (int)(object)value!);
		public static T RemoveFlag<T>(this System.Enum type, T value) => (T)(object)((int)(object)type & ~(int)(object)value!);
		public static T ToggleFlag<T>(this System.Enum type, T value) => (T)(object)((int)(object)type ^ (int)(object)value!);
		public static T ToggleFlag<T>(this System.Enum type, params T[] values) {
			var temp = (int)(object)type;
			temp = values.Aggregate(temp, (current, value) => current ^ (int)(object)value!);
			return (T)(object)temp;
		}
	}
}