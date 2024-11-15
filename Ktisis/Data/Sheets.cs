using System;
using System.Collections.Generic;

using Lumina.Excel;

namespace Ktisis.Data {
	internal class Sheets {
		// Sheets

		internal static Dictionary<Type, IExcelSheet> Cache = new();

		public static ExcelSheet<T> GetSheet<T>() where T : struct, IExcelRow<T> {
			var type = typeof(T);
			if (Cache.TryGetValue(type, out var value))
				return (ExcelSheet<T>)value;

			var sheet = Services.DataManager.GetExcelSheet<T>();
			Cache.Add(type, sheet);
			return sheet;
		}

		public static void ClearSheet<T>() where T : struct, IExcelRow<T> {
			var type = typeof(T);
			if (Cache.ContainsKey(type))
				Cache.Remove(type);
		}
	}
}
