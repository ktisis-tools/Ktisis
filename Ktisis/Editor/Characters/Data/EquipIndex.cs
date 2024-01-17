using System;

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
	public static EquipIndex ToEquipIndex(this EquipSlot slot) => slot switch {
		< EquipSlot.Waist => (EquipIndex)slot,
		> EquipSlot.Waist => (EquipIndex)(slot - 1),
		_ => throw new Exception("Invalid value for equipment conversion.")
	};
}