using System.Collections.Generic;
using System.Linq;

using Lumina.Excel;

using Ktisis.Structs.Actor;
using Ktisis.Structs.Extensions;

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
	public struct Item : IExcelRow<Item> {
		public uint RowId { get; }
		
		public string Name { get; set; } = "";
		public int Icon { get; set; }

		public RowRef<EquipSlotCategory> EquipSlotCategory { get; set; }

		public ItemModel Model { get; set; } = null!;
		public ItemModel SubModel { get; set; } = null!;

		public bool IsEquippable() => EquipSlotCategory.IsValid && EquipSlotCategory.RowId != 0;
		public bool IsEquippable(EquipSlot slot) => IsEquippable() && EquipSlotCategory.Value.IsEquippable(slot);

		public bool IsWeapon() => IsEquippable(EquipSlot.MainHand) || IsEquippable(EquipSlot.OffHand);

		public Item(uint row, ExcelPage page, uint offset) {
			this.RowId = row;
			
			this.Name = page.ReadColumn<string>(9, offset);
			this.Icon = page.ReadColumn<ushort>(10, offset);

			this.EquipSlotCategory = page.ReadRowRef<EquipSlotCategory>(17, offset);

			var isWep = this.IsWeapon();
			this.Model = new ItemModel(page.ReadColumn<ulong>(47, offset), isWep);
			this.SubModel = new ItemModel(page.ReadColumn<ulong>(48, offset), isWep);
		}

		public static Item Create(ExcelPage page, uint offset, uint row) => new(row, page, offset);

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
	public struct EquipSlotCategory(uint row) : IExcelRow<EquipSlotCategory> {
		public uint RowId => row;
		
		public sbyte[] Slots { get; set; } = new sbyte[14];

		public bool IsEquippable(EquipSlot slot) => Slots[(int)slot] == 1 || (slot == EquipSlot.MainHand && Slots[1] == 1) || (slot == EquipSlot.OffHand && Slots[0] == 1);

		public static EquipSlotCategory Create(ExcelPage page, uint offset, uint row) {
			return new EquipSlotCategory(row) {
				Slots = ReadSlots(page, offset).ToArray()
			};
		}

		private static IEnumerable<sbyte> ReadSlots(ExcelPage page, uint offset) {
			for (var i = 0; i < 14; i++)
				yield return page.ReadColumn<sbyte>(i, offset);
		}
	}
}
