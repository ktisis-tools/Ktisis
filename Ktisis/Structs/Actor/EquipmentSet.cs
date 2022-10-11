using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Dalamud.Logging;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using Ktisis.GameData;
using Ktisis.GameData.Excel;

namespace Ktisis.Structs.Actor {
	public struct EquipmentSet
	{
		public int ID;
		public SetSource Source;
		public string Name;
		public EquipmentSet(int iD, string name, SetSource source)
		{
			ID = iD;
			Name = name;
			Source = source;
		}
	}

	public class EquipmentSets
	{
		public List<EquipmentSet> Sets;
		private IEnumerable<Item> ItemsSheet;

		public EquipmentSets(IEnumerable<Item> itemsSheet)
		{
			ItemsSheet = itemsSheet;
			Sets = new();
		}

		// helpers
		private Item GetEmperorNewItemForSlot(EquipIndex equipIndex) => ItemsSheet!.Where(i => i.IsEquippable(Equipment.EquipIndexToItemSlot(equipIndex)) && i.Name.Contains("Emperor's New")).First(); // TODO: improve performances, maybe cache them in constructor

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
			if(Sets.Any()) return true;
			return false;
		}
		public List<EquipmentSet> GetSets() => Sets;
		public void RefreshItemSheet(IEnumerable<Item> itemsSheet) => ItemsSheet = itemsSheet;


		// Set Finders
		private List<EquipmentSet> FindSets()
		{
			return      FindSetsGearSet()
				.Concat(FindSetsGlamourPlate())
				.Concat(FindSetsGlamourer())
				.Concat(FindSetsGlamaholic())
				.ToList();
		}
		private unsafe List<EquipmentSet> FindSetsGearSet()
		{
			List<EquipmentSet> sets = new();
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
		private List<EquipmentSet> FindSetsGlamourPlate()
		{
			return new List<EquipmentSet>();
		}
		private List<EquipmentSet> FindSetsGlamourer()
		{
			return new List<EquipmentSet>();
		}
		private List<EquipmentSet> FindSetsGlamaholic()
		{
			return new List<EquipmentSet>();
		}


		// Find items of the selected set
		public List<(EquipIndex, EquipItem)> GetItems(EquipmentSet set)
		{
			List<(EquipIndex index, EquipItem equip)> items = new();
			switch (set.Source)
			{
				case SetSource.GearSet: items = GetItemsGearset(set); break;
				case SetSource.GlamourDresser: items = GetItemsGlamourDresser(set); break;
			}
			return items;
		}
		private unsafe List<(EquipIndex index, EquipItem equip)> GetItemsGearset(EquipmentSet set)
		{
			List<(EquipIndex index, EquipItem equip)> itemsToEquip = new();
			var gearset = RaptureGearsetModule.Instance()->Gearset[set.ID];

			if (gearset->GlamourSetLink > 0)
				// TODO: implement glamour plates for:   return GetItemsGlamourDresser(set);
				PluginLog.Warning("Gearset Linked to Glamour Plate " + gearset->GlamourSetLink + ", Glamour Plate support not implemented");


			// find inventory containers
			InventoryType[] inventoryTypes =
			{
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
			List<(uint id, EquipIndex ind)> itemsToRemodel = new()
			{
				(gearset->Head.ItemID, EquipIndex.Head),
				(gearset->Body.ItemID, EquipIndex.Chest),
				(gearset->Hands.ItemID, EquipIndex.Hands),
				(gearset->Legs.ItemID, EquipIndex.Legs),
				(gearset->Feet.ItemID, EquipIndex.Feet),
				(gearset->Ears.ItemID, EquipIndex.Earring),
				(gearset->Neck.ItemID, EquipIndex.Necklace),
				(gearset->Wrists.ItemID, EquipIndex.Bracelet),
				(gearset->RingRight.ItemID, EquipIndex.RingRight),
				(gearset->RightLeft.ItemID, EquipIndex.RingLeft) // rightleft? :x
			};

			foreach ((uint id, EquipIndex ind) in itemsToRemodel)
			{

				Item? item = null;
				InventoryItem? invItem = null;

				if (id != 0)
				{
					// get the inventory item by the gearset item id
					var invItems = inventoryItems.Where(i => i.ItemID == id);
					if (!invItems.Any()) invItems = inventoryItems.Where(i => i.ItemID == uint.Parse(id.ToString()[2..])); // not sure why, sometimes item IDs have numbers prepended to them (mostly "10")
					if (invItems.Any()) invItem = invItems.First();

					// get the Item that contains the model Id
					var items = Sheets.GetSheet<Item>().Where(i => i.RowId == (invItem?.GlamourID == 0 ? invItem?.ItemID : invItem?.GlamourID));
					if (items.Any()) item = items.First();
				}

				// if no item found, choose "The Emperor's New ..." in this slot
				item ??= GetEmperorNewItemForSlot(ind);
				byte dye = (invItem?.Stain) ?? default;


				EquipItem newItem = new()
				{
					Id = (item?.Model.Id) ?? 0,
					Variant = (byte)((item?.Model.Variant) ?? 0),
					Dye = dye,
				};
				itemsToEquip.Add((ind, newItem));
			}

			return itemsToEquip;
		}
		private List<(EquipIndex index, EquipItem equip)> GetItemsGlamourDresser(EquipmentSet set)
		{
			throw new NotImplementedException();
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