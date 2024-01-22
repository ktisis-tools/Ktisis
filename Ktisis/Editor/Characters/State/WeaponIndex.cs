using System;

using Ktisis.GameData.Excel;

namespace Ktisis.Editor.Characters.State;

public enum WeaponIndex : uint {
	MainHand = 0,
	OffHand = 1,
	Prop = 2
}

public static class WeaponIndexEx {
	public static EquipSlot ToEquipSlot(this WeaponIndex index) => index switch {
		< WeaponIndex.Prop => (EquipSlot)index,
		WeaponIndex.Prop => EquipSlot.OffHand,
		_ => throw new Exception($"Cannot convert invalid weapon index ({index})")
	};
}