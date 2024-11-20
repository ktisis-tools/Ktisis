using System;
using System.Collections.Generic;
using System.Linq;

using Newtonsoft.Json;

using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI;

using Ktisis.Data.Excel;
using Ktisis.Structs.FFXIV;

namespace Ktisis.Structs.Actor.Equip.SetSources
{
	// This class will handle the interaction between different aspects
	// of Glamour plates data: hook, storage and lookup
	public class GlamourDresser
	{

		// Change this constant if Glamour Plates Number is changed in the Game.
		// TODO: find this variable from the game's memory
		public const int _platesNumber = 20;

		// In this variable will be stored the plates,
		// it will be updated when loading configurations
		// and detecting a change
		internal static GlamourPlate[]? Plates = null;



		internal static Dictionary<int, string> List() {
			// no smarts here, we just make the plate list if it has been populated
			// TODO: if not too resource heavy, each plate could be checked if it is
			// Populated or not with the isValid() method

			Dictionary<int, string> nameList = new();
			if (Plates != null)
				for (int i = 1; i <= _platesNumber; i++)
					nameList.Add(i, $"Glamour Plate {i}");

			return nameList;
		}
		internal static List<(EquipSlot, object)> GetItemsForSet(Set set) {
			List<(EquipSlot, object)> itemsToEquip = new();
			var plates = Plates;
			if (plates == null) throw new NotImplementedException();


			foreach (var plateItem in plates[set.ID - 1].Items) {
				var itemId = plateItem.ItemId;
				var dyeId = plateItem.DyeId;
				var slot = GlamourPlateSlotToEquipSlot(plateItem.Slot);

				if (itemId == 0) {
					// if slot is left empty remove the item
					itemsToEquip.Add((slot, Sets.EmptySlot(slot)));
				}

				Item? item = null;
				var items = Sets.ItemsSheet.Where(i => i.RowId == itemId);
				if (items.Any()) item = items.First();

				if (itemId == 0 || item == null) {
					itemsToEquip.Add((slot, Sets.EmptySlot(slot)));
					continue;
				}
				itemsToEquip.Add((slot, Sets.ItemToEquipObject(item, dyeId, slot)));
			}

			return itemsToEquip;
		}




		public static int CountValid() {
			if (Plates == null) return 0;
			if (Plates.Length == 0) return 0;

			return Plates.Count((p) => p.IsValid());
		}

		public static void PopulatePlatesData() {
			var local_character_id = Services.ClientState.LocalContentId;
			if (local_character_id == 0) return;

			var platesBefore = Plates;
			GetDataFromDresser(); // if the plate window is open, read it
			if (Plates == platesBefore) PopupOfferOpenGlamourPlates_open(); // if unsuccessfull, proceed with active dialog
			else SavePlatesToConfig();
		}
		private static bool SavePlatesToConfig() {
			if (Plates == null) return false;

			if (Ktisis.Configuration.GlamourPlateData == null)
				Ktisis.Configuration.GlamourPlateData = new Dictionary<string, GlamourPlate[]?>();
			if (Ktisis.Configuration.GlamourPlateData == null) return false;

			var character_key = $"FFXIV_CHR{Services.ClientState.LocalContentId:X16}";
			Ktisis.Configuration.GlamourPlateData.TryGetValue(character_key, out var platesFromConfig);
			if (Plates == platesFromConfig) return false;

			Ktisis.Configuration.GlamourPlateData[character_key] = Plates; // actually save

			bool success = Ktisis.Configuration.GlamourPlateData[character_key] == Plates;
			if (success) Logger.Verbose($"Saved {Ktisis.Configuration.GlamourPlateData[character_key]!.Count((p) => p.IsValid())} Plates for {character_key} into config.");
			return true;
		}
		public static void LoadGlamourPlatesIntoMemory() {
			var local_character_id = Services.ClientState.LocalContentId;
			if (local_character_id == 0) return;

			if (Ktisis.Configuration.GlamourPlateData == null) return;
			var character_key = $"FFXIV_CHR{local_character_id:X16}";
			Ktisis.Configuration.GlamourPlateData!.TryGetValue(character_key, out Plates);
			Logger.Verbose($"Loaded {CountValid()} Plates for {character_key} into memory.");
		}

		internal static void PopupOfferOpenGlamourPlates_open() {
			// This is a way to actively get the data, with the user's authorization
			if (UIGlobals.CanApplyGlamourPlates())
				Interface.Components.Equipment.OpenGlamourQuestionPopup();
		}
		public static EquipSlot GlamourPlateSlotToEquipSlot(GlamourPlateSlot slot) => (EquipSlot)((int)slot + ((int)slot > 4 ? 1 : 0));

		internal static unsafe void GetDataFromDresser() {
			var agent = MiragePrismMiragePlate.MiragePlateAgent();
			if (agent == null) return;
			var miragePlates = (MiragePrismMiragePlate*)agent;
			if (!miragePlates->AgentInterface.IsAgentActive()) return;

			var platePages = miragePlates->Pages;
			Plates = new GlamourPlate[_platesNumber];

			for (int plateNumber = 0; plateNumber < Plates.Length; ++plateNumber)
				Plates[plateNumber] = new GlamourPlate(platePages[plateNumber]);
		}


		// Serializable classes to save MiragePage[] in configuration.
		[Serializable]
		public class GlamourPlate {
			public GlamourPlateItem[] Items = new GlamourPlateItem[12];
			public bool IsValid() => Items.Any((i) => i.IsValid());

			// Json Configuration constructor
			[JsonConstructor]
			public GlamourPlate(GlamourPlateItem[] items) {
				Items = items;
			}

			// pseudo "cast" constructor to convert Game struct into serializable
			internal GlamourPlate(MiragePage plate) {
				var fields = typeof(MiragePage).GetFields();
				for (int slot = 0; slot < fields.Length; slot++) {
					MirageItem item = (MirageItem)fields[slot].GetValue(plate)!;
					Items[slot] = new GlamourPlateItem(item, (GlamourPlateSlot)slot);
				}
			}
		}

		[Serializable]
		public class GlamourPlateItem {
			public uint ItemId { get; set; }
			public byte DyeId { get; set; }
			public GlamourPlateSlot Slot { get; set; }
			public bool IsValid() => ItemId != 0;

			// Json Configuration constructor
			[JsonConstructor]
			public GlamourPlateItem(uint itemId, byte dyeId, GlamourPlateSlot slot) {
				ItemId = itemId;
				DyeId = dyeId;
				Slot = slot;
			}

			// pseudo "cast" constructor to convert Game struct into serializable
			internal GlamourPlateItem(MirageItem item, GlamourPlateSlot slot) {
				ItemId = item.ItemId;
				DyeId = item.DyeId;
				Slot = slot;
			}
		}

		public enum GlamourPlateSlot : uint {
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
	}
}
