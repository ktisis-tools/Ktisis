using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Dalamud.Logging;
using FFXIVClientStructs.Attributes;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Ktisis.GameData.Excel;
using Newtonsoft.Json;

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
		public List<EquipmentSet> GetSets() => Sets;
		public void RefreshItemSheet(IEnumerable<Item> itemsSheet) => ItemsSheet = itemsSheet;

		public static void Init()
		{
			EquipmentSetSources.GlamourDresser.LoadGlamourPlatesIntoMemory();
		}
		public static void Dispose()
		{
			Interface.Windows.ActorEdit.EditEquip.Sets = null;
			EquipmentSetSources.GlamourDresser.Plates = null;
			PluginLog.Verbose("Disposed Sets and Plates");
		}

		// Set Finders
		private List<EquipmentSet> FindSets()
		{
			return FindSetsGearSet()
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
			var sets = new List<EquipmentSet>();

			// no smarts here, we just make the plate list if it has been populated
			if (EquipmentSetSources.GlamourDresser.Plates != null)
				for (int i = 1; i <= EquipmentSetSources.GlamourDresser._platesNumber; i++)
					sets.Add(new EquipmentSet(i, $"Glamour Plate {i}", SetSource.GlamourDresser));

			return sets;
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
		public List<(EquipSlot, object)> GetItems(EquipmentSet set)
		{
			List<(EquipSlot, object)> items = new();
			switch (set.Source)
			{
				case SetSource.GearSet: items = GetItemsGearset(set); break;
				case SetSource.GlamourDresser: items = GetItemsGlamourDresser(set); break;
			}
			return items;
		}
		private unsafe List<(EquipSlot, object)> GetItemsGearset(EquipmentSet set)
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
				if (id == 0) {
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

				if(item == null) {
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

		public static bool FindAllValidEquip((EquipSlot, object)a)
		{
			var item = a.Item2;
			if (item is WeaponEquip w)
				return w.Set != 0;
			else if (item is ItemEquip e)
				return e.Id != 0;
			return false;
		}
		private List<(EquipSlot, object)> GetItemsGlamourDresser(EquipmentSet set)
		{
			List<(EquipSlot, object)> itemsToEquip = new();
			var plates = EquipmentSetSources.GlamourDresser.Plates;
			if (plates == null) throw new NotImplementedException();


			foreach (var plateItem in plates[set.ID - 1].Items)
			{
				var itemId = plateItem.ItemId;
				var dyeId = plateItem.DyeId;
				var slot = EquipmentSetSources.GlamourDresser.GlamourPlateSlotToEquipSlot(plateItem.Slot);

				if(itemId == 0) {
					// if slot is left empty remove the item
					itemsToEquip.Add((slot, EmptySlot(slot)));
				}

				Item? item = null;
				var items = ItemsSheet.Where(i => i.RowId == itemId);
				if (items.Any()) item = items.First();

				if (itemId == 0 || item == null) {
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

// Sources specific classes

namespace Ktisis.Structs.Actor.EquipmentSetSources
{
	public class GlamourDresser {

		public const int _platesNumber = 20;
		public static GlamourPlate[]? Plates = null;

		public static int CountValid()
		{
			if(Plates == null) return 0;
			if(Plates.Length == 0) return 0;

			return Plates.Count((p) => p.IsValid())!;
		}

		public static void PopulatePlatesData()
		{
			var local_character_id = Dalamud.ClientState.LocalContentId;
			if (local_character_id == 0) return;

			var platesBefore = Plates;
			GetDataFromDresser(); // if the plate window is open, read it
			if (Plates == platesBefore) PopupOfferOpenGlamourPlates_open(); // if unsuccessfull, proceed with active dialog
			else SavePlatesToConfig();
		}
		private static bool SavePlatesToConfig()
		{
			if (Plates == null) return false;

			if (Ktisis.Configuration.GlamourPlateData == null)
				Ktisis.Configuration.GlamourPlateData = new Dictionary<string, GlamourPlate[]?>();
			if (Ktisis.Configuration.GlamourPlateData == null) return false;

			var character_key = $"FFXIV_CHR{Dalamud.ClientState.LocalContentId:X16}";
			Ktisis.Configuration.GlamourPlateData.TryGetValue(character_key, out var platesFromConfig);
			if (Plates == platesFromConfig) return false;

			Ktisis.Configuration.GlamourPlateData[character_key] = Plates; // actually save

			bool success = Ktisis.Configuration.GlamourPlateData[character_key] == Plates;
			if (success) PluginLog.Verbose($"Saved {Ktisis.Configuration.GlamourPlateData[character_key]!.Count((p) => p.IsValid())} Plates for {character_key} into config.");
			return true;
		}
		public static void LoadGlamourPlatesIntoMemory()
		{
			var local_character_id = Dalamud.ClientState.LocalContentId;
			if (local_character_id == 0) return;

			if (Ktisis.Configuration.GlamourPlateData == null) return;
			var character_key = $"FFXIV_CHR{local_character_id:X16}";
			Ktisis.Configuration.GlamourPlateData!.TryGetValue(character_key, out Plates);
			PluginLog.Verbose($"Loaded {CountValid()} Plates for {character_key} into memory.");
		}

		internal static void PopupOfferOpenGlamourPlates_open()
		{
			// This is a way to actively get the data, with the user's authorization
			if(GameMain.IsInSanctuary())
				Interface.Components.Equipment.OpenGlamourQuestionPopup();
		}
		internal static unsafe void PopupOfferOpenGlamourPlates_confirmed()
		{
			var agent = MiragePlateAgent();
			agent->Show();
		}

		internal static unsafe AgentInterface* MiragePlateAgent() => Framework.Instance()->GetUiModule()->GetAgentModule()->GetAgentByInternalId(AgentId.MiragePrismMiragePlate);
		public static EquipSlot GlamourPlateSlotToEquipSlot(GlamourPlateSlot slot) => (EquipSlot)((int)slot + ((int)slot > 4 ? 1 : 0));

		internal static unsafe void GetDataFromDresser()
		{
			var agent = MiragePlateAgent();
			if (agent == null) return;
			var miragePlates = (MiragePrismMiragePlates*)agent;
			if (!miragePlates->AgentInterface.IsAgentActive()) return;

			var platePages = miragePlates->Pages;
			Plates = new GlamourPlate[_platesNumber];

			for (int plateNumber = 0; plateNumber < Plates.Length; ++plateNumber)
				Plates[plateNumber] = new GlamourPlate(platePages[plateNumber]);
		}

		[Serializable]
		public class GlamourPlate
		{
			public GlamourPlateItem[] Items = new GlamourPlateItem[12];
			public bool IsValid() => Items.Any((i) => i.IsValid());

			[JsonConstructor]
			public GlamourPlate(GlamourPlateItem[] items)
			{
				Items = items;
			}

			internal GlamourPlate(Plate plate)
			{
				var fields = typeof(Plate).GetFields();
				for (int slot = 0; slot < fields.Length; slot++)
				{
					MirageItem item = (MirageItem)fields[slot].GetValue(plate)!;
					Items[slot] = new GlamourPlateItem(item, (GlamourPlateSlot)slot);
				}
			}

		}

		[Serializable]
		public class GlamourPlateItem
		{
			public uint ItemId { get; set; }
			public byte DyeId { get; set; }
			public GlamourPlateSlot Slot { get; set; }
			public bool IsValid() => ItemId != 0;

			[JsonConstructor]
			public GlamourPlateItem(uint itemId, byte dyeId, GlamourPlateSlot slot)
			{
				ItemId = itemId;
				DyeId = dyeId;
				Slot = slot;
			}

			internal GlamourPlateItem(MirageItem item, GlamourPlateSlot slot)
			{
				ItemId = item.ItemId;
				DyeId = item.DyeId;
				Slot = slot;
			}

		}

		public enum GlamourPlateSlot : uint
		{
			MainHand = 0,
			OffHand = 1,
			Head = 2,
			Body = 3,
			Hands = 4,
			Legs = 5,
			Feet = 6,
			Ears = 7,
			Neck = 8,
			Wrists = 9,
			RightRing = 10,
			LeftRing = 11,
		}


		// Game structs
		[Agent(AgentId.MiragePrismMiragePlate)]
		[StructLayout(LayoutKind.Explicit)]
		public unsafe partial struct MiragePrismMiragePlates
		{

			[FieldOffset(0)] public AgentInterface AgentInterface;
			//[FieldOffset(40 + 36)] public IntPtr* PlatesPointer;

			public Plate[] Pages
			{
				get
				{
					var totalPages = _platesNumber +1 ; // the currently viewing/editing page is added at the end of the array
					Plate[] pages = new Plate[totalPages];

					if(!AgentInterface.IsAgentActive()) return pages; ;

					// TODO: find a way to use PlatesPointer instead of calling the agent again
					var agent = MiragePlateAgent();
					var glamPlatePointer = *(IntPtr*)((IntPtr)agent + 40) + 36;

					for (int plateNumber = 0; plateNumber < totalPages; plateNumber++)
					{
						var offset = 44 * 12 * plateNumber;
						pages[plateNumber] = *(Plate*)(glamPlatePointer + offset);

					}
					return pages;
				}
			}
		}

		[StructLayout(LayoutKind.Explicit, Size = 0x210)]
		public struct Plate
		{
			[FieldOffset(0x2C * 00)] public MirageItem MainHand;
			[FieldOffset(0x2C * 01)] public MirageItem OffHand;
			[FieldOffset(0x2C * 02)] public MirageItem Head;
			[FieldOffset(0x2C * 03)] public MirageItem Chest;
			[FieldOffset(0x2C * 04)] public MirageItem Hands;
			[FieldOffset(0x2C * 05)] public MirageItem Legs;
			[FieldOffset(0x2C * 06)] public MirageItem Feet;
			[FieldOffset(0x2C * 07)] public MirageItem Earring;
			[FieldOffset(0x2C * 08)] public MirageItem Necklace;
			[FieldOffset(0x2C * 09)] public MirageItem Bracelet;
			[FieldOffset(0x2C * 10)] public MirageItem RingRight;
			[FieldOffset(0x2C * 11)] public MirageItem RingLeft;
		}

		// Thanks to Anna's Glamaholic code
		// for showing the logic behind the Glamour Plates <3
		[StructLayout(LayoutKind.Explicit, Size = 44)]
		public struct MirageItem
		{
			[FieldOffset(0)] public uint ItemId;
			//[FieldOffset(4)] public uint Unk1; // > 0 when previewing an item
			//[FieldOffset(8)] public uint Unk2; // = 1 when previwing item
			//[FieldOffset(12)] public uint Unk3;
			//[FieldOffset(16)] public uint Unk4;
			[FieldOffset(20)] public uint ItemType; // not item slot
			[FieldOffset(24)] public byte DyeId;
			[FieldOffset(25)] public byte DyePreviewId;
			//[FieldOffset(26)] public byte Unk5; // = 1 when previwing item
			//[FieldOffset(28)] public uint Unk7; // > 0 when previewing item + dye
			//[FieldOffset(39)] public byte Unk8; // = 1 when previewing item + dye
			//[FieldOffset(42)] public ushort Unk9;
		}
	}

}