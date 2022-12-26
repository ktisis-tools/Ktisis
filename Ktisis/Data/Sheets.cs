using System;
using System.Collections.Generic;

using Ktisis.Services;

using Lumina.Excel;

namespace Ktisis.Data {
	internal class Sheets {
		// Sheets

		internal static Dictionary<Type, ExcelSheetImpl> Cache = new();

		public static ExcelSheet<T> GetSheet<T>() where T : ExcelRow {
			var type = typeof(T);
			if (Cache.ContainsKey(type))
				return (ExcelSheet<T>)Cache[type];

			var sheet = DalamudServices.DataManager.GetExcelSheet<T>()!;
			Cache.Add(type, sheet);
			return sheet;
		}

		public static void ClearSheet<T>() where T : ExcelRow {
			var type = typeof(T);
			if (Cache.ContainsKey(type))
				Cache.Remove(type);
		}
	}
}