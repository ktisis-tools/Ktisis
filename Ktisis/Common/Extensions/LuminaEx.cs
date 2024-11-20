using System;

using FFXIVClientStructs.FFXIV.Client.Game.Character;

using Ktisis.Structs.Characters;

using Lumina.Excel;
using Lumina.Data.Parsing;
using Lumina.Data.Structs.Excel;

namespace Ktisis.Common.Extensions;

public static class LuminaEx {
	// Lumina 5

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
	
	// Sheet helpers
	
	public static CustomizeContainer ReadCustomize(this ExcelPage parser, int index, uint offset) {
		var result = new CustomizeContainer();
		for (var i = 0; i < CustomizeContainer.Size; i++)
			result[(uint)i] = parser.ReadColumn<byte>(index + i, offset);
		return result;
	}
	
	public static WeaponModelId ReadWeapon(this ExcelPage parser, int index, uint offset) {
		var quad = (Quad)parser.ReadColumn<ulong>(index, offset);
		var dye = parser.ReadColumn<byte>(index + 1, offset);
		var dye2 = parser.ReadColumn<byte>(index + 2, offset);
		return new WeaponModelId {
			Id = quad.A,
			Type = quad.B,
			Variant = quad.C,
			Stain0 = dye,
			Stain1 = dye2
		};
	}

	public static EquipmentModelId ReadEquipItem(this ExcelPage parser, int index, uint offset) {
		var model = parser.ReadColumn<uint>(index, offset);
		var dye = parser.ReadColumn<byte>(index + 1, offset);
		var dye2 = parser.ReadColumn<byte>(index + 2, offset);
		return new EquipmentModelId {
			Id = (ushort)model,
			Variant = (byte)(model >> 16),
			Stain0 = dye,
			Stain1 = dye2
		};
	}

	public static EquipmentContainer ReadEquipment(this ExcelPage parser, int index, uint offset) {
		var result = new EquipmentContainer();
		for (var i = 0; i < EquipmentContainer.Length; i++)
			result[(uint)i] = parser.ReadEquipItem(index + i * 3 + (i > 0 ? 1 : 0), offset);
		return result;
	}
}
