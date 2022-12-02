using System.Linq;
using System.Text;
using System.Collections.Generic;

using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;

using Ktisis.Data.Excel;

namespace Ktisis.Structs.Actor.Equip.SetSources {
	public class GearSet {

		const int _gearSetNumber = 101; // Max number on Simple Tweaks

		public static unsafe Dictionary<int,string> List() {
			Dictionary<int, string> nameList = new();
			var raptureGearsetModule = RaptureGearsetModule.Instance();

			for (var i = 0; i < _gearSetNumber; i++) {
				var gearset = raptureGearsetModule->Gearset[i];
				if (gearset->ID != i) break;
				if (!gearset->Flags.HasFlag(RaptureGearsetModule.GearsetFlag.Exists)) continue;
				nameList.Add(i, Encoding.UTF8.GetString(gearset->Name, 0x2F));
			}

			return nameList;
		}

		public static unsafe List<(EquipSlot, object)> GetEquipForSet(Set set) {
			List<(EquipSlot, object)> itemsToEquip = new();
			var gearset = RaptureGearsetModule.Instance()->Gearset[set.ID];

			// find inventory containers
			InventoryType[] inventoryTypes = {
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
			List<(uint, EquipSlot)> itemsToRemodel = new() {
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


			foreach ((uint id, EquipSlot slot) in itemsToRemodel) {
				if (id == 0) {
					// if gearset slot is not set
					itemsToEquip.Add((slot, Sets.EmptySlot(slot)));
					continue;
				}

				Item? item = null;
				InventoryItem? invItem = null;

				// get the inventory item by the gearset item id
				var invItems = inventoryItems.Where(i => i.ItemID == id);
				if (!invItems.Any()) invItems = inventoryItems.Where(i => i.ItemID == uint.Parse(id.ToString()[2..])); // not sure why, sometimes item IDs have numbers prepended to them (mostly "10")
				if (invItems.Any()) invItem = invItems.First();

				// get the Item that contains the model Id
				var items = Sets.ItemsSheet.Where(i => i.RowId == (invItem?.GlamourID == 0 ? invItem?.ItemID : invItem?.GlamourID));
				if (items.Any()) item = items.First();

				if (item == null) {
					// if gearset slot is set, but item wasn't found in inventories
					itemsToEquip.Add((slot, Sets.EmptySlot(slot)));
					continue;
				}

				byte dye = (invItem?.Stain) ?? default;
				itemsToEquip.Add((slot, Sets.ItemToEquipObject(item, dye, slot)));
			}

			if (gearset->GlamourSetLink == 0) return itemsToEquip; // not linked to

			var glamourItems = Sets.GetItemsGlamourDresser(Sets.FindSetsGlamourPlate().Find(s => s.ID == gearset->GlamourSetLink));

			// overwrite gearset items with valid items from the glam plate
			foreach (var tGlam in glamourItems.FindAll(Sets.FindAllValidEquip)) // keep only items that are not 0
			{
				int index = itemsToEquip.FindIndex(t => tGlam.Item1 == t.Item1);
				if (index >= 0)
					itemsToEquip[index] = tGlam;
			}
			return itemsToEquip;
		}
	}
}
