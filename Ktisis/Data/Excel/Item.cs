using Lumina.Data;
using Lumina.Text;
using Lumina.Excel;

using Ktisis.Structs.Actor;

namespace Ktisis.Data.Excel {
	public enum EquipSlot {
		MainHand,
		OffHand,
		Head,
		Chest,
		Hands,
		Waist,
		Legs,
		Feet,
		Earring,
		Necklace,
		Bracelet,
		RingLeft,
		RingRight,
		SoulCrystal
	}

	public class ItemModel {
		public ushort Id { get; set; }
		public ushort Base { get; set; }
		public ushort Variant { get; set; }

		public ItemModel(ulong var, bool isWep = false) {
			Id = (ushort)var;
			Base = (ushort)(isWep ? var >> 16 : 0);
			Variant = (ushort)(isWep ? var >> 32 : var >> 16);
		}
	}

	[Sheet("Item")]
	public class Item : ExcelRow {
		public string Name { get; set; } = "";
		public ushort Icon { get; set; }

		public LazyRow<EquipSlotCategory> EquipSlotCategory { get; set; } = null!;

		public ItemModel Model { get; set; } = null!;
		public ItemModel SubModel { get; set; } = null!;

		public bool IsEquippable() => EquipSlotCategory.Value!.RowId != 0;
		public bool IsEquippable(EquipSlot slot) => IsEquippable() && EquipSlotCategory.Value!.IsEquippable(slot);

		public bool IsWeapon() => IsEquippable(EquipSlot.MainHand) || IsEquippable(EquipSlot.OffHand);

		public override void PopulateData(RowParser parser, Lumina.GameData gameData, Language language) {
			base.PopulateData(parser, gameData, language);

			Name = parser.ReadColumn<SeString>(9) ?? "";
			Icon = parser.ReadColumn<ushort>(10);

			EquipSlotCategory = new LazyRow<EquipSlotCategory>(gameData, parser.ReadColumn<byte>(17), language);

			var isWep = IsWeapon();
			var model = parser.ReadColumn<ulong>(47);
			var subModel = parser.ReadColumn<ulong>(48);
			Model = new ItemModel(model, isWep);
			SubModel = new ItemModel(subModel, isWep);
		}

		public bool IsEquipItem(object equip) {
			if (equip is WeaponEquip wep) {
				if (Model.Id == 0 && SubModel.Id == 0) return false;
				var m1 = wep.Set == Model.Id && wep.Base == Model.Base && wep.Variant == Model.Variant;
				var m2 = SubModel.Id != 0 && wep.Set == SubModel.Id && wep.Base == SubModel.Base && wep.Variant == SubModel.Variant;
				return m1 || m2;
			}
			if (equip is ItemEquip item)
				return item.Id == Model.Id && item.Variant == Model.Variant;
			return equip.Equals(this);
		}
	}

	[Sheet("EquipSlotCategory")]
	public class EquipSlotCategory : ExcelRow {
		public sbyte[] Slots { get; set; } = new sbyte[14];

		public bool IsEquippable(EquipSlot slot) => Slots[(int)slot] == 1 || (slot == EquipSlot.MainHand && Slots[1] == 1) || (slot == EquipSlot.OffHand && Slots[0] == 1);

		public override void PopulateData(RowParser parser, Lumina.GameData gameData, Language language) {
			base.PopulateData(parser, gameData, language);

			for (var i = 0; i < 14; i++)
				Slots[i] = parser.ReadColumn<sbyte>(i);
		}
	}
}