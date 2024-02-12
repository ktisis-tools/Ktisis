using FFXIVClientStructs.FFXIV.Client.Game.Character;

using Ktisis.Structs.Characters;

using Lumina.Data.Parsing;
using Lumina.Excel;

namespace Ktisis.Common.Extensions;

public static class RowParserEx {
	public static CustomizeContainer ReadCustomize(this RowParser parser, int index) {
		var result = new CustomizeContainer();
		for (var i = 0; i < CustomizeContainer.Size; i++)
			result[(uint)i] = parser.ReadColumn<byte>(index + i);
		return result;
	}
	
	public static WeaponModelId ReadWeapon(this RowParser parser, int index) {
		var quad = parser.ReadStructure<Quad>(index);
		var dye = parser.ReadColumn<byte>(index + 1);
		return new WeaponModelId {
			Id = quad.A,
			Type = quad.B,
			Variant = quad.C,
			Stain = dye
		};
	}

	public static EquipmentModelId ReadEquipItem(this RowParser parser, int index) {
		var model = parser.ReadColumn<uint>(index);
		var dye = parser.ReadColumn<byte>(index + 1);
		return new EquipmentModelId {
			Id = (ushort)model,
			Variant = (byte)(model >> 16),
			Stain = dye
		};
	}

	public static EquipmentContainer ReadEquipment(this RowParser parser, int index) {
		var result = new EquipmentContainer();
		for (var i = 0; i < EquipmentContainer.Length; i++)
			result[(uint)i] = parser.ReadEquipItem(index + i * 2 + (i > 0 ? 1 : 0));
		return result;
	}
}
