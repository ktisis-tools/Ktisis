using Ktisis.Common.Extensions;

using Lumina.Excel;

namespace Ktisis.GameData.Excel;

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
	SoulCrystal,
	Glasses
}

public class ItemModel(ulong var, bool isWep = false) {
	public ushort Id = (ushort)var;
	public ushort Base = (ushort)(isWep ? var >> 16 : 0);
	public ushort Variant = (ushort)(isWep ? var >> 32 : var >> 16);

	public bool Matches(ushort id, ushort variant)
		=> this.Id == id && this.Variant == variant;

	public bool Matches(ushort id, ushort secondId, ushort variant)
		=> this.Id == id && this.Base == secondId && this.Variant == variant;
}

[Sheet("Item", columnHash: 0xe9a33c9d)]
public struct ItemSheet : IExcelRow<ItemSheet> {
	public uint RowId { get; }

	public string Name { get; }

	public ushort Icon { get; }

	public ItemModel Model { get; }
	public ItemModel SubModel { get; }

	private RowRef<EquipSlotCategoryRow> EquipSlotCategory { get; }

	public bool IsEquippable() => this.EquipSlotCategory.IsValid && this.EquipSlotCategory.RowId != 0;
	public bool IsEquippable(EquipSlot slot) {
		var result = this.IsEquippable() && this.EquipSlotCategory.Value.IsEquippable(slot);
		if (slot == EquipSlot.MainHand)
			result |= this.EquipSlotCategory.Value.IsEquippable(EquipSlot.OffHand);
		return result;
	}

	public bool IsWeapon() => this.IsEquippable(EquipSlot.MainHand) || this.IsEquippable(EquipSlot.OffHand);

	public ItemSheet(ExcelPage page, uint offset, uint row) {
		this.RowId = row;

		this.Name = page.ReadColumn<string>(9, offset);
		this.Icon = page.ReadColumn<ushort>(10, offset);
		
		this.EquipSlotCategory = page.ReadRowRef<EquipSlotCategoryRow>(17, offset);

		var isWep = this.IsWeapon();
		this.Model = new ItemModel(page.ReadColumn<ulong>(47, offset), isWep);
		this.SubModel = new ItemModel(page.ReadColumn<ulong>(48, offset), isWep);
	}

	static ItemSheet IExcelRow<ItemSheet>.Create(ExcelPage page, uint offset, uint row) => new(page, offset, row);
	
	// Equip slots

	[Sheet("EquipSlotCategory")]
	private struct EquipSlotCategoryRow(uint row) : IExcelRow<EquipSlotCategoryRow> {
		public uint RowId { get; } = row;

		private bool[] Slots { get; set; } = new bool[14];

		public bool IsEquippable(EquipSlot slot) => slot switch {
			EquipSlot.MainHand => this.Slots[1],
			EquipSlot.OffHand => this.Slots[0],
			_ => this.Slots[(int)slot]
		};

		static EquipSlotCategoryRow IExcelRow<EquipSlotCategoryRow>.Create(ExcelPage page, uint offset, uint row) {
			var slots = new bool[14];
			for (var i = 0; i < 14; i++)
				slots[i] = page.ReadColumn<sbyte>(i, offset) != 0;
			return new EquipSlotCategoryRow(row) {
				Slots = slots
			};
		}
	}
}
