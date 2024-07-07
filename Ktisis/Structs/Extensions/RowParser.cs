using Lumina.Excel;
using Lumina.Data.Parsing;

using Ktisis.Structs.Actor;

namespace Ktisis.Structs.Extensions {
	public static class RowParserExtensions {
		public static Customize ReadCustomize(this RowParser parser, int index) {
			var result = new byte[Customize.Length];
			for (var i = 0; i < Customize.Length; i++)
				result[i] = parser.ReadColumn<byte>(index + i);
			return Customize.FromBytes(result);
		}
		
		public static WeaponEquip ReadWeapon(this RowParser parser, int index) {
			var data = parser.ReadColumn<ulong>(index);
			var dye = parser.ReadColumn<byte>(index + 1);
			var dye2 = parser.ReadColumn<byte>(index + 2);

			var quad = (Quad)data;
			return new WeaponEquip {
				Set = quad.A,
				Base = quad.B,
				Variant = quad.C,
				Dye = dye,
				Dye2 = dye2
			};
		}

		public static ItemEquip ReadItem(this RowParser parser, int index) {
			var model = parser.ReadColumn<uint>(index);
			var dye = parser.ReadColumn<byte>(index + 1);
			var dye2 = parser.ReadColumn<byte>(index + 2);
			
			return new ItemEquip {
				Id = (ushort)model,
				Variant = (byte)(model >> 16),
				Dye = dye,
				Dye2 = dye2
			};
		}

		public unsafe static Equipment ReadEquipment(this RowParser parser, int index) {
			var result = new Equipment();
			for (var i = 0; i < Equipment.SlotCount; i++)
				result.Slots[i] = (ulong)parser.ReadItem(index + i * 3 + (i > 0 ? 1 : 0));
			return result;
		}
	}
}
