using Lumina;
using Lumina.Data;
using Lumina.Excel;
using Lumina.Text;

namespace Ktisis.Data.Excel;

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

public class ItemModel(ulong var, bool isWep = false) {
	public ushort Id = (ushort)var;
	public ushort Base = (ushort)(isWep ? var >> 16 : 0);
	public ushort Variant = (ushort)(isWep ? var >> 32 : var >> 16);
}

[Sheet("Item")]
public class ItemRow : ExcelRow {
	public string Name { get; set; } = string.Empty;

	public ushort Icon { get; set; }

	public ItemModel Model { get; set; } = null!;
	public ItemModel SubModel { get; set; } = null!;

	private LazyRow<EquipSlotCategoryRow> EquipSlotCategory { get; set; } = null!;

	public bool IsEquippable() => this.EquipSlotCategory.Row != 0;
	public bool IsEquippable(EquipSlot slot) => this.IsEquippable() && this.EquipSlotCategory.Value?.IsEquippable(slot) == true;

	public bool IsWeapon() => this.IsEquippable(EquipSlot.MainHand) || this.IsEquippable(EquipSlot.OffHand);

	public override void PopulateData(RowParser parser, GameData gameData, Language language) {
		base.PopulateData(parser, gameData, language);

		this.Name = parser.ReadColumn<SeString>(9) ?? string.Empty;
		this.Icon = parser.ReadColumn<ushort>(10);

		this.EquipSlotCategory = new LazyRow<EquipSlotCategoryRow>(gameData, parser.ReadColumn<byte>(17), language);

		var isWep = this.IsWeapon();
		this.Model = new ItemModel(parser.ReadColumn<ulong>(47), isWep);
		this.SubModel = new ItemModel(parser.ReadColumn<ulong>(48), isWep);
	}
	
	// Equip slots

	[Sheet("EquipSlotCategory")]
	private class EquipSlotCategoryRow : ExcelRow {
		private bool[] Slots { get; set; } = new bool[14];

		public bool IsEquippable(EquipSlot slot) => slot switch {
			EquipSlot.MainHand => this.Slots[1],
			EquipSlot.OffHand => this.Slots[0],
			_ => this.Slots[(int)slot]
		};

		public override void PopulateData(RowParser parser, GameData gameData, Language language) {
			base.PopulateData(parser, gameData, language);
			for (var i = 0; i < 14; i++)
				this.Slots[i] = parser.ReadColumn<sbyte>(i) == 1;
		}
	}
}
