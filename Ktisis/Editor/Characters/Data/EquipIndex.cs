using Ktisis.Data.Excel;

namespace Ktisis.Editor.Characters.Data;

public enum EquipIndex : uint {
	Head,
	Chest,
	Hands,
	Legs,
	Feet,
	Earring,
	Necklace,
	Bracelet,
	RingRight,
	RingLeft
}

public static class EquipIndexEx {
	public static EquipSlot ToEquipSlot(this EquipIndex index) {
		var value = (int)index;
		return index switch {
			EquipIndex.RingLeft => EquipSlot.RingLeft,
			EquipIndex.RingRight => EquipSlot.RingRight,
			_ => (EquipSlot)(value + (value > 2 ? 3 : 2))
		};
	}

	public static EquipIndex ToEquipIndex(this EquipSlot slot) {
		var value = (int)slot;
		return slot switch {
			EquipSlot.RingLeft => EquipIndex.RingLeft,
			EquipSlot.RingRight => EquipIndex.RingRight,
			_ => (EquipIndex)(value - (value >= 5 ? 3 : 2))
		};
	}
}