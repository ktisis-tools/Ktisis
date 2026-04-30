using System;

using Dalamud.Utility;

using Lumina.Data.Structs.Excel;
using Lumina.Excel;

namespace Ktisis.Structs.Extensions {
	public static class ExcelPageExtensions {
		public static RowRef<T> ReadRowRef<T>(this ExcelPage page, int columnIndex, uint offset) where T : struct, IExcelRow<T> {
			var rowId = GetAndReadColumn(page, columnIndex, offset);
			return new RowRef<T>(page.Module, Convert.ToUInt32(rowId), page.Language);
		}
		
		public static T ReadColumn<T>(this ExcelPage page, int columnIndex, uint offset) {
			return (T)GetAndReadColumn(page, columnIndex, offset);
		}

		private static object GetAndReadColumn(ExcelPage page, int columnIndex, uint offset) {
			var column = page.Sheet.Columns[columnIndex];
			return column.Type switch {
				ExcelColumnDataType.String => page.ReadString(column.Offset + offset, offset).ExtractText(),
				ExcelColumnDataType.Bool => page.ReadBool(column.Offset + offset),
				ExcelColumnDataType.Int8 => page.ReadInt8(column.Offset + offset),
				ExcelColumnDataType.UInt8 => page.ReadUInt8(column.Offset + offset),
				ExcelColumnDataType.Int16 => page.ReadInt16(column.Offset + offset),
				ExcelColumnDataType.UInt16 => page.ReadUInt16(column.Offset + offset),
				ExcelColumnDataType.Int32 => page.ReadInt32(column.Offset + offset),
				ExcelColumnDataType.UInt32 => page.ReadUInt32(column.Offset + offset),
				ExcelColumnDataType.Float32 => page.ReadFloat32(column.Offset + offset),
				ExcelColumnDataType.Int64 => page.ReadInt64(column.Offset + offset),
				ExcelColumnDataType.UInt64 => page.ReadUInt64(column.Offset + offset),
				ExcelColumnDataType.PackedBool0 => page.ReadPackedBool(column.Offset + offset, 0),
				ExcelColumnDataType.PackedBool1 => page.ReadPackedBool(column.Offset + offset, 1),
				ExcelColumnDataType.PackedBool2 => page.ReadPackedBool(column.Offset + offset, 2),
				ExcelColumnDataType.PackedBool3 => page.ReadPackedBool(column.Offset + offset, 3),
				ExcelColumnDataType.PackedBool4 => page.ReadPackedBool(column.Offset + offset, 4),
				ExcelColumnDataType.PackedBool5 => page.ReadPackedBool(column.Offset + offset, 5),
				ExcelColumnDataType.PackedBool6 => page.ReadPackedBool(column.Offset + offset, 6),
				ExcelColumnDataType.PackedBool7 => page.ReadPackedBool(column.Offset + offset, 7),
				_ => throw new Exception($"Unknown type: {column.Type}")
			};
		}
	}
}