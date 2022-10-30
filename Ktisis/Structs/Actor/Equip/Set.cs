using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Dalamud.Logging;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;

using Ktisis.GameData.Excel;

namespace Ktisis.Structs.Actor.Equip
{
	public struct Set
	{
		public int ID;
		public SetSource Source;
		public string Name;
		public Set(int iD, string name, SetSource source)
		{
			ID = iD;
			Name = name;
			Source = source;
		}
	}

	public class EquipmentSets
	{
		public List<Set> Sets;
		public IEnumerable<Item> ItemsSheet;

		public EquipmentSets(IEnumerable<Item> itemsSheet)
		{
			ItemsSheet = itemsSheet;
			Sets = new();
		}

		// helpers
		private static object EmptySlot(EquipSlot equipSlot)
		{
			if (equipSlot == EquipSlot.MainHand || equipSlot == EquipSlot.OffHand)
				return new WeaponEquip();
			return new ItemEquip();
		}

		// public managers
		public static EquipmentSets InitAndLoadSources(IEnumerable<Item> itemsSheet)
		{
			var sets = new EquipmentSets(itemsSheet!);
			sets.LoadSources();
			return sets;
		}
		public bool LoadSources()
		{
			Sets = FindSets();
			if (Sets.Any()) return true;
			return false;
		}
		public List<Set> GetSets() => Sets;
		public void RefreshItemSheet(IEnumerable<Item> itemsSheet) => ItemsSheet = itemsSheet;

		public static void Init()
		{
			SetSources.GlamourDresser.LoadGlamourPlatesIntoMemory();
		}
		public static void Dispose()
		{
			Interface.Windows.ActorEdit.EditEquip.Sets = null;
			SetSources.GlamourDresser.Plates = null;
			PluginLog.Verbose("Disposed Sets and Plates");
		}

		// Set Finders
		private List<Set> FindSets()
		{
			return FindSetsGearSet()
				.Concat(FindSetsGlamourPlate())
				.Concat(FindSetsGlamourer())
				.Concat(FindSetsGlamaholic())
				.ToList();
		}
		private unsafe List<Set> FindSetsGearSet()
		{
			List<Set> sets = new();
			var raptureGearsetModule = RaptureGearsetModule.Instance();

			// build sets list
			for (var i = 0; i < 101; i++)
			{
				var gearset = raptureGearsetModule->Gearset[i];
				if (gearset->ID != i) break;
				if (!gearset->Flags.HasFlag(RaptureGearsetModule.GearsetFlag.Exists)) continue;
				sets.Add(new(i, Encoding.UTF8.GetString(gearset->Name, 0x2F), SetSource.GearSet));
			}

			return sets;
		}
		private List<Set> FindSetsGlamourPlate()
		{
			var sets = new List<Set>();

			// no smarts here, we just make the plate list if it has been populated
			if (SetSources.GlamourDresser.Plates != null)
				for (int i = 1; i <= SetSources.GlamourDresser._platesNumber; i++)
					sets.Add(new Set(i, $"Glamour Plate {i}", SetSource.GlamourDresser));

			return sets;
		}
		private List<Set> FindSetsGlamourer()
		{
			return new List<Set>();
		}
		private List<Set> FindSetsGlamaholic()
		{
			return new List<Set>();
		}


		// Find items of the selected set
		public List<(EquipSlot, object)> GetItems(Set set)
		{
			List<(EquipSlot, object)> items = new();
			switch (set.Source)
			{
				case SetSource.GearSet: items = GetItemsGearset(set); break;
				case SetSource.GlamourDresser: items = GetItemsGlamourDresser(set); break;
			}
			return items;
		}
		private unsafe List<(EquipSlot, object)> GetItemsGearset(Set set)
		{
			List<(EquipSlot, object)> itemsToEquip = new();
			var gearset = RaptureGearsetModule.Instance()->Gearset[set.ID];

			// find inventory containers
			InventoryType[] inventoryTypes =
			{
				InventoryType.ArmoryMainHand,
				InventoryType.ArmoryOffHand,
				InventoryType.EquippedItems,
				InventoryType.ArmoryHead,
				InventoryType.ArmoryBody,
				InventoryType.ArmoryHands,
				InventoryType.ArmoryLegs,
				InventoryType.ArmoryFeets,
				InventoryType.ArmoryEar,
				InventoryType.ArmoryNeck,
				InventoryType.ArmoryWrist,
				InventoryType.ArmoryRings

			};

			InventoryContainer*[] Armouries = new InventoryContainer*[inventoryTypes.Length];
			for (int j = 0; j < inventoryTypes.Length; j++)
				Armouries[j] = InventoryManager.Instance()->GetInventoryContainer(inventoryTypes[j]);

			// generate a list of all combined inventories
			List<InventoryItem> inventoryItems = new();
			foreach (InventoryContainer* Armoury in Armouries)
				for (int i = 0; i < Armoury->Size; i++)
					inventoryItems.Add(Armoury->Items[i]);

			// get item IDs from gearset
			List<(uint, EquipSlot)> itemsToRemodel = new()
			{
				(gearset->MainHand.ItemID, EquipSlot.MainHand),
				(gearset->OffHand.ItemID, EquipSlot.OffHand),
				(gearset->Head.ItemID, EquipSlot.Head),
				(gearset->Body.ItemID, EquipSlot.Chest),
				(gearset->Hands.ItemID, EquipSlot.Hands),
				(gearset->Legs.ItemID, EquipSlot.Legs),
				(gearset->Feet.ItemID, EquipSlot.Feet),
				(gearset->Ears.ItemID, EquipSlot.Earring),
				(gearset->Neck.ItemID, EquipSlot.Necklace),
				(gearset->Wrists.ItemID, EquipSlot.Bracelet),
				(gearset->RightLeft.ItemID, EquipSlot.RingLeft),
				(gearset->RingRight.ItemID, EquipSlot.RingRight),
			};


			foreach ((uint id, EquipSlot slot) in itemsToRemodel)
			{
				if (id == 0)
				{
					// if gearset slot is not set
					itemsToEquip.Add((slot, EmptySlot(slot)));
					continue;
				}

				Item? item = null;
				InventoryItem? invItem = null;

				// get the inventory item by the gearset item id
				var invItems = inventoryItems.Where(i => i.ItemID == id);
				if (!invItems.Any()) invItems = inventoryItems.Where(i => i.ItemID == uint.Parse(id.ToString()[2..])); // not sure why, sometimes item IDs have numbers prepended to them (mostly "10")
				if (invItems.Any()) invItem = invItems.First();

				// get the Item that contains the model Id
				var items = ItemsSheet.Where(i => i.RowId == (invItem?.GlamourID == 0 ? invItem?.ItemID : invItem?.GlamourID));
				if (items.Any()) item = items.First();

				if (item == null)
				{
					// if gearset slot is set, but item wasn't found in inventories
					itemsToEquip.Add((slot, EmptySlot(slot)));
					continue;
				}

				byte dye = (invItem?.Stain) ?? default;
				itemsToEquip.Add((slot, ItemToEquipObject(item, dye, slot)));
			}

			if (gearset->GlamourSetLink == 0) return itemsToEquip; // not linked to

			var glamourItems = GetItemsGlamourDresser(Sets.Find(s => s.ID == gearset->GlamourSetLink));

			// overwrite gearset items with valid items from the glam plate
			foreach (var tGlam in glamourItems.FindAll(FindAllValidEquip)) // keep only items that are not 0
			{
				int index = itemsToEquip.FindIndex(t => tGlam.Item1 == t.Item1);
				if (index >= 0)
					itemsToEquip[index] = tGlam;
			}
			return itemsToEquip;
		}

		public static bool FindAllValidEquip((EquipSlot, object) a)
		{
			var item = a.Item2;
			if (item is WeaponEquip w)
				return w.Set != 0;
			else if (item is ItemEquip e)
				return e.Id != 0;
			return false;
		}
		private List<(EquipSlot, object)> GetItemsGlamourDresser(Set set)
		{
			List<(EquipSlot, object)> itemsToEquip = new();
			var plates = SetSources.GlamourDresser.Plates;
			if (plates == null) throw new NotImplementedException();


			foreach (var plateItem in plates[set.ID - 1].Items)
			{
				var itemId = plateItem.ItemId;
				var dyeId = plateItem.DyeId;
				var slot = SetSources.GlamourDresser.GlamourPlateSlotToEquipSlot(plateItem.Slot);

				if (itemId == 0)
				{
					// if slot is left empty remove the item
					itemsToEquip.Add((slot, EmptySlot(slot)));
				}

				Item? item = null;
				var items = ItemsSheet.Where(i => i.RowId == itemId);
				if (items.Any()) item = items.First();

				if (itemId == 0 || item == null)
				{
					itemsToEquip.Add((slot, EmptySlot(slot)));
					continue;
				}
				itemsToEquip.Add((slot, ItemToEquipObject(item, dyeId, slot)));
			}

			return itemsToEquip;
		}
		public static object ItemToEquipObject(Item? item, byte dyeId, EquipSlot slot)
		{
			var id = (item?.Model.Id) ?? 0;
			var variant = (byte)((item?.Model.Variant) ?? 0);

			if (slot == EquipSlot.MainHand || slot == EquipSlot.OffHand)
				return new WeaponEquip
				{
					Set = id,
					Base = (item?.Model.Base) ?? 0,
					Dye = dyeId,
					Variant = variant,
				};
			else
				return new ItemEquip
				{
					Id = id,
					Variant = variant,
					Dye = dyeId,
				};
		}
	}

	public enum SetSource
	{
		GearSet,
		GlamourDresser,
		Glamaholic,
		Glamourer,
	};

}
