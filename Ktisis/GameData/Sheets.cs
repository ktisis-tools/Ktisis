using System;
using System.Collections.Generic;

using Dalamud.Data;

using Lumina;
using Lumina.Excel;

using Ktisis.GameData.Excel;

namespace Ktisis.GameData {
	internal class Sheets {
		internal static Lumina.GameData Data { get; set; } = null!;

		public static void Init(Lumina.GameData data) {
			Data = data;
		}

		// Sheets

		internal static Dictionary<Type, ExcelSheetImpl> Cache = new();

		public static ExcelSheet<T> GetSheet<T>() where T : ExcelRow {
			var type = typeof(T);
			if (Cache.ContainsKey(type))
				return (ExcelSheet<T>)Cache[type];

			var sheet = Data.GetExcelSheet<T>()!;
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