using System.Collections.Generic;

using Lumina;
using Lumina.Data;
using Lumina.Text;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;

using Ktisis.Structs.Actor;

namespace Ktisis.GameData.Excel {
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

	public struct ItemModel {
		public ushort Id;
		public ushort Base;
		public ushort Variant;

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

		public ItemModel Model { get; set; }
		public ItemModel SubModel { get; set; }

		public bool IsEquippable() => EquipSlotCategory.Value!.RowId != 0;
		public bool IsEquippable(EquipSlot slot) => IsEquippable() && EquipSlotCategory.Value!.IsEquippable(slot);

		public bool IsWeapon => IsEquippable(EquipSlot.MainHand) || IsEquippable(EquipSlot.OffHand);

		public override void PopulateData(RowParser parser, Lumina.GameData gameData, Language language) {
			RowId = parser.RowId;

			Name = parser.ReadColumn<SeString>(9) ?? "";
			Icon = parser.ReadColumn<ushort>(10);

			EquipSlotCategory = new LazyRow<EquipSlotCategory>(gameData, parser.ReadColumn<byte>(17), language);

			var isWep = IsWeapon;
			var model = parser.ReadColumn<ulong>(47);
			var subModel = parser.ReadColumn<ulong>(48);
			Model = new ItemModel(model, isWep);
			SubModel = new ItemModel(subModel, isWep);
		}
	}

	[Sheet("EquipSlotCategory")]
	public class EquipSlotCategory : ExcelRow {
		public List<sbyte> Slots { get; set; } = new();

		public bool IsEquippable(EquipSlot slot) => Slots[(int)slot] == 1;

		public override void PopulateData(RowParser parser, Lumina.GameData gameData, Language language) {
			base.PopulateData(parser, gameData, language);
			for (var i = 0; i < 14; i++)
				Slots.Add(parser.ReadColumn<sbyte>(i));
		}
	}
}