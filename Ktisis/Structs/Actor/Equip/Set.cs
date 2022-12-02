using System.Linq;
using System.Collections.Generic;

using Ktisis.Data;
using Ktisis.Data.Excel;

namespace Ktisis.Structs.Actor.Equip {
	public struct Set {
		public int ID;
		public SetSource Source;
		public string Name;
		public Set(int iD, string name, SetSource source) {
			ID = iD;
			Name = name;
			Source = source;
			// TODO: add visibility booleans for weapon, hat and visor
		}
	}

	public enum SetSource {
		GearSet,
		GlamourDresser,
		Glamaholic,
		Glamourer,
	};

	// This class is where are made the all the connections between
	// the selection lists, the data lookup and data storage.
	public static class Sets {
		internal static IEnumerable<Item> ItemsSheet = Sheets.GetSheet<Item>();

		// Init and Dispose
		public static void Init() {
			SetSources.GlamourDresser.LoadGlamourPlatesIntoMemory();
		}
		public static void Dispose() {
			SetSources.GlamourDresser.Plates = null;
		}

		// Main methods, called by the Interface EditEquip
		public static List<Set> FindSets() {
			return FindSetsGearSet()
				.Concat(FindSetsGlamourPlate())
				.Concat(FindSetsGlamourer())
				.Concat(FindSetsGlamaholic())
				.ToList();
		}
		public static List<(EquipSlot, object)> GetItems(Set set) {

			List<(EquipSlot, object)> items = new();
			switch (set.Source) {
				case SetSource.GearSet: items = GetItemsGearset(set); break;
				case SetSource.GlamourDresser: items = GetItemsGlamourDresser(set); break;
			}
			return items;
		}

		// The methods with a name starting with FindSets (e.g. FindSetsGearSet)
		// will be called when creating the selectable list.
		private static List<Set> FindSetsGearSet() => SetSources.GearSet.List().Select((i) => new Set(
				i.Key,
				i.Value,
				SetSource.GearSet)
			).ToList();
		internal static List<Set> FindSetsGlamourPlate() => SetSources.GlamourDresser.List().Select((i) => new Set(
				i.Key,
				i.Value,
				SetSource.GlamourDresser)
			).ToList();
		private static List<Set> FindSetsGlamourer() => new();
		private static List<Set> FindSetsGlamaholic() => new();

		// The methods with a name starting with GetItems (e.g. GetItemsGearSet)
		// will be called when selecting an item in the selectable list.
		// It will retrieve the information on items and dyes
		private static List<(EquipSlot, object)> GetItemsGearset(Set set) =>
			SetSources.GearSet.GetEquipForSet(set);
		internal static List<(EquipSlot, object)> GetItemsGlamourDresser(Set set) =>
			SetSources.GlamourDresser.GetItemsForSet(set);

		// helpers methods, typically used in SetSources namespace
		internal static object EmptySlot(EquipSlot equipSlot) {
			if (equipSlot == EquipSlot.MainHand || equipSlot == EquipSlot.OffHand)
				return new WeaponEquip();
			return new ItemEquip();
		}

		internal static object ItemToEquipObject(Item? item, byte dyeId, EquipSlot slot) {
			var id = (item?.Model.Id) ?? 0;
			var variant = (byte)((item?.Model.Variant) ?? 0);

			if (slot == EquipSlot.MainHand || slot == EquipSlot.OffHand) {
				return new WeaponEquip {
					Set = id,
					Base = (item?.Model.Base) ?? 0,
					Dye = dyeId,
					Variant = variant,
				};
			} else {
				return new ItemEquip {
					Id = id,
					Variant = variant,
					Dye = dyeId,
				};
			}
		}

		internal static bool FindAllValidEquip((EquipSlot, object) a) {
			var item = a.Item2;
			if (item is WeaponEquip w)
				return w.Set != 0;
			else if (item is ItemEquip e)
				return e.Id != 0;
			return false;
		}
	}
}
