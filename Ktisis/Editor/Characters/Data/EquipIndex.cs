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
		var res = (EquipSlot)(value + (value > 2 ? 3 : 2));
		return res;
	}

	public static EquipIndex ToEquipIndex(this EquipSlot slot) {
		var value = (int)slot;
		var res = (EquipIndex)(value - (value >= 5 ? 3 : 2));
		return res;
	}
}