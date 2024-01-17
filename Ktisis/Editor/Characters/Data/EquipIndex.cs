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
		return (EquipSlot)(value + (value >= 3 ? 3 : 2));
	}

	public static EquipIndex ToEquipIndex(this EquipSlot slot) {
		var value = (int)slot;
		return (EquipIndex)(value - (value >= 3 ? 3 : 2));
	}
}