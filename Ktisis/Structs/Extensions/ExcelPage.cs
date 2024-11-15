using System;

using Dalamud.Utility;

using Lumina.Data.Structs.Excel;
using Lumina.Excel;

namespace Ktisis.Structs.Extensions {
	public static class ExcelPageExtensions {
		public static RowRef<T> ReadRowRef<T>(this ExcelPage page, int columnIndex) where T : struct, IExcelRow<T> {
			var rowId = page.ReadColumn<uint>(columnIndex);
			return new RowRef<T>(page.Module, rowId, page.Language);
		}
		
		public static T ReadColumn<T>(this ExcelPage page, int columnIndex, uint offset = 0) {
			return (T)GetAndReadColumn(page, columnIndex, offset);
		}

		private static object GetAndReadColumn(ExcelPage page, int columnIndex, uint offset = 0) {
			var column = page.Sheet.Columns[columnIndex];
			return column.Type switch {
				ExcelColumnDataType.String => page.ReadString(column.Offset, offset).ToDalamudString().TextValue,
				ExcelColumnDataType.Bool => page.ReadBool(column.Offset),
				ExcelColumnDataType.Int8 => page.ReadInt8(column.Offset),
				ExcelColumnDataType.UInt8 => page.ReadUInt8(column.Offset),
				ExcelColumnDataType.Int16 => page.ReadInt16(column.Offset),
				ExcelColumnDataType.UInt16 => page.ReadUInt16(column.Offset),
				ExcelColumnDataType.Int32 => page.ReadInt32(column.Offset),
				ExcelColumnDataType.UInt32 => page.ReadUInt32(column.Offset),
				ExcelColumnDataType.Float32 => page.ReadFloat32(column.Offset),
				ExcelColumnDataType.Int64 => page.ReadInt64(column.Offset),
				ExcelColumnDataType.UInt64 => page.ReadUInt64(column.Offset),
				ExcelColumnDataType.PackedBool0 => page.ReadPackedBool(column.Offset, 0),
				ExcelColumnDataType.PackedBool1 => page.ReadPackedBool(column.Offset, 1),
				ExcelColumnDataType.PackedBool2 => page.ReadPackedBool(column.Offset, 2),
				ExcelColumnDataType.PackedBool3 => page.ReadPackedBool(column.Offset, 3),
				ExcelColumnDataType.PackedBool4 => page.ReadPackedBool(column.Offset, 4),
				ExcelColumnDataType.PackedBool5 => page.ReadPackedBool(column.Offset, 5),
				ExcelColumnDataType.PackedBool6 => page.ReadPackedBool(column.Offset, 6),
				ExcelColumnDataType.PackedBool7 => page.ReadPackedBool(column.Offset, 7),
				_ => throw new Exception($"Unknown type: {column.Type}")
			};
		}
	}
}