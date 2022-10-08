using System;
using System.Linq;
using System.Numerics;
using System.Collections.Generic;

using ImGuiNET;
using ImGuiScene;

using Dalamud.Logging;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.FFXIV.Client.Game;

using Ktisis.GameData;
using Ktisis.GameData.Excel;
using Ktisis.Structs.Actor;
using System.Text;
using Ktisis.Util;
using Dalamud.Interface;

namespace Ktisis.Interface.Windows.ActorEdit {
	public class EditEquip {
		// Constants

		public const int _IconSize = 36;
		public static Vector2 IconSize = new(_IconSize, _IconSize);

		// Properties

		public unsafe static Actor* Target => EditActor.Target;

		public static IEnumerable<Item>? Items;

		public static Dictionary<EquipSlot, ItemCache> Equipped = new();

		public static Vector2 SelectPos;
		public static EquipSlot? SlotSelect;
		public static IEnumerable<Item>? SlotItems;
		public static string ItemSearch = "";
		public static int? LastSelectedItemKey = null;
		public static bool DrawSetSelection = false;

		// Helper stuff. Will move if there's ever a need for this elsewhere.

		public static Item? FindItem(EquipItem item, EquipSlot slot)
			=> Items?.FirstOrDefault(i => i.IsEquippable(slot) && i.Model.Id == item.Id && i.Model.Variant == item.Variant, null!);

		public static EquipIndex SlotToIndex(EquipSlot slot) => (EquipIndex)(slot - ((int)slot >= 5 ? 3 : 2));

		// UI Code

		public unsafe static void Draw() {
			FindSets();
			if (Items == null)
				Items = Sheets.GetSheet<Item>().Where(i => i.IsEquippable());


			ImGui.BeginGroup();
			for (var i = 2; i < 13; i++) {
				var slot = (EquipSlot)i;
				if (slot == EquipSlot.Waist) continue;
				if (i == 8) {
					ImGui.EndGroup();
					ImGui.SameLine();
					ImGui.BeginGroup();
				}
				DrawSelector(slot);
			}
			ImGui.EndGroup();

			ImGui.EndTabItem();
		}

		public unsafe static void DrawSelector(EquipSlot slot) {
			var tar = EditActor.Target;
			var index = SlotToIndex(slot);

			var equip = (EquipItem)tar->Equipment.Slots[(int)index];
			if (!Equipped.ContainsKey(slot)) {
				Equipped.Add(slot, new() {
					EquipItem = equip,
					Item = FindItem(equip, slot)
				});
			} else if (!Equipped[slot].EquipItem.Equals(equip)) {
				Equipped[slot].EquipItem = equip;
				Equipped[slot].Item = FindItem(equip, slot);
				Equipped[slot].Icon = null;
			}

			var item = Equipped[slot];

			if (item.Icon == null)
				item.Icon = Dalamud.DataManager.GetImGuiTextureIcon(item.Item == null ? (uint)0 : item.Item.Icon);

			if (ImGui.ImageButton(item.Icon!.ImGuiHandle, IconSize) && SlotSelect == null)
				OpenSelector(slot);

			ImGui.SameLine();
			ImGui.BeginGroup();

			var name = item.Item == null ? "Unknown" : item.Item.Name;
			ImGui.Text(name);

			ImGui.PushItemWidth(100);
			var val = new int[2] { equip.Id, equip.Variant };
			if (ImGui.InputInt2($"##{slot}", ref val[0])) {
				equip.Id = (ushort)val[0];
				equip.Variant = (byte)val[1];
				tar->Equip(index, equip);
			}
			ImGui.PopItemWidth();

			ImGui.EndGroup();

			if (SlotSelect == slot)
				DrawSelectorList(index, equip);
		}

		public static void OpenSelector(EquipSlot slot) {
			SlotSelect = slot;
			SlotItems = Items!.Where(i => i.IsEquippable(slot));
			SelectPos = ImGui.GetMousePos();
		}

		public static void OpenSetSelector() {
			DrawSetSelection = true;
			SelectPos = ImGui.GetMousePos();
		}

		public unsafe static void DrawSelectorList(EquipIndex index, EquipItem equip) {
			if (SlotItems == null)
				return;

			var size = new Vector2(-1, -1);
			ImGui.SetNextWindowSize(size, ImGuiCond.Always);

			ImGui.SetNextWindowPos(SelectPos);
			ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(10, 10));

			if (ImGui.Begin("Item Select", ImGuiWindowFlags.NoDecoration)) {
				var focus = ImGui.IsWindowFocused() || ImGui.IsWindowHovered();

				ImGui.PushItemWidth(400);
				ImGui.InputTextWithHint("##equip_search", "Search...", ref ItemSearch, 32);
				ImGui.BeginListBox("##equip_items", new Vector2(-1, 300));
				// TODO: scroll the list to the currently selected item when using ImGuiKey

				var items = SlotItems;
				if (ItemSearch.Length > 0)
					items = items.Where(i => i.Name.Contains(ItemSearch, StringComparison.OrdinalIgnoreCase));

				int itemKey = 0;
				bool isAnItemSelected = false; // allows one selection per foreach

				foreach (var item in items) {
					itemKey++;
					// TODO: Icon?

					// TODO: mark the currently selected item
					bool selecting = false;

					selecting |= ImGui.Selectable($"{item.Name}");
					selecting |= ImGui.IsKeyPressed(ImGuiKey.RightShift) && !isAnItemSelected && itemKey == LastSelectedItemKey - 1;
					selecting |= ImGui.IsKeyPressed(ImGuiKey.RightCtrl) && !isAnItemSelected && itemKey == LastSelectedItemKey + 1;

					if (selecting) {
						equip.Id = item.Model.Id;
						equip.Variant = (byte)item.Model.Variant;
						Target->Equip(index, equip);
						LastSelectedItemKey = itemKey;
						isAnItemSelected = true;
					}
					focus |= ImGui.IsItemFocused();
				}
				ImGui.EndListBox();
				focus |= ImGui.IsItemActive();
				ImGui.PopItemWidth();

				if (!focus) {
					SlotSelect = null;
					SlotItems = null;
				}
			}

			ImGui.End();
		}

		public static unsafe void FindSets()
		{
			List<SetLookup> sets = new();
			var raptureGearsetModule = RaptureGearsetModule.Instance();

			// build sets list
			for (var i = 0; i < 101; i++)
			{
				var gearset = raptureGearsetModule->Gearset[i];
				if (gearset->ID != i) break;
				if (!gearset->Flags.HasFlag(RaptureGearsetModule.GearsetFlag.Exists)) continue;
				sets.Add(new(i, Encoding.UTF8.GetString(gearset->Name, 0x2F), SetSource.GearSet));
			}

			if(GuiHelpers.IconButtonTooltip(FontAwesomeIcon.Tshirt, "Look up for a set."))
				OpenSetSelector();

			if (DrawSetSelection)
				DrawSetSelectorList(sets);

			ImGui.Separator();
		}

		// dirty shameless copy paste of DrawSelectorList() TODO: merge common code?
		public unsafe static void DrawSetSelectorList(List<SetLookup> sets)
		{
			if (sets.Count == 0)
				return;

			var size = new Vector2(-1, -1);
			ImGui.SetNextWindowSize(size, ImGuiCond.Always);

			ImGui.SetNextWindowPos(SelectPos);
			ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(10, 10));

			if (ImGui.Begin("Set Select", ImGuiWindowFlags.NoDecoration))
			{
				var focus = ImGui.IsWindowFocused() || ImGui.IsWindowHovered();

				ImGui.PushItemWidth(400);
				string setSearch = "";
				ImGui.InputTextWithHint("##equip_search", "Search...", ref setSearch, 32);
				ImGui.BeginListBox("##equip_items", new Vector2(-1, 300));
				// TODO: scroll the list to the currently selected item when using ImGuiKey

				if (setSearch.Length > 0)
					sets = sets.Where(s => s.Name.Contains(setSearch, StringComparison.OrdinalIgnoreCase)).ToList();

				int itemKey = 0;
				bool isAnItemSelected = false; // allows one selection per foreach

				foreach (var item in sets)
				{
					itemKey++;
					// TODO: Icon?

					// TODO: mark the currently selected item
					bool selecting = false;

					selecting |= ImGui.Selectable($"{item.Name}");
					selecting |= ImGui.IsKeyPressed(ImGuiKey.RightShift) && !isAnItemSelected && itemKey == LastSelectedItemKey - 1;
					selecting |= ImGui.IsKeyPressed(ImGuiKey.RightCtrl) && !isAnItemSelected && itemKey == LastSelectedItemKey + 1;

					if (selecting)
					{
						EquipSet(item);

						LastSelectedItemKey = itemKey;
						isAnItemSelected = true;
					}
					focus |= ImGui.IsItemFocused();
				}
				ImGui.EndListBox();
				focus |= ImGui.IsItemActive();
				ImGui.PopItemWidth();

				if (!focus)
				{
					DrawSetSelection = false;
				}
			}

			ImGui.End();
		}
		
		public static unsafe void EquipSet(SetLookup setLookup)
		{
			PluginLog.Verbose("equipping set");

			List<(EquipIndex index, EquipItem equip)> itemsToEquip = new();
			switch (setLookup.Source)
			{
				case SetSource.GearSet        : itemsToEquip = EquipSetGearset(setLookup);        break;
				case SetSource.GlamourDresser : itemsToEquip = EquipSetGlamourDresser(setLookup); break;
			}

			foreach((EquipIndex i, EquipItem e) in itemsToEquip)
			{
				Target->Equip(i, e);
			}
		}
		public static unsafe List<(EquipIndex index, EquipItem equip)> EquipSetGearset(SetLookup setLookup)
		{
			List<(EquipIndex index, EquipItem equip)> itemsToEquip = new();
			var gearset = RaptureGearsetModule.Instance()->Gearset[setLookup.ID];

			if(gearset->GlamourSetLink > 0)
				// TODO: implement glamour plates for:   return EquipSetGlamourDresser(setLookup);
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
			InventoryContainer*[] Armouries = new InventoryContainer *[inventoryTypes.Length];
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
					var invItems = inventoryItems.Where(i => i.ItemID == id );
					if (!invItems.Any()) invItems = inventoryItems.Where(i => i.ItemID == uint.Parse(id.ToString()[2..])); // not sure why, sometimes item IDs have numbers prepended to them (mostly "10")
					if (invItems.Any())	invItem = invItems.First();

					// get the Item that contains the model Id
					var items = Sheets.GetSheet<Item>().Where(i => i.RowId == (invItem?.GlamourID == 0 ? invItem?.ItemID: invItem?.GlamourID));
					if (items.Any()) item = items.First();
				}

				// if no item found, choose "The Emperor's New ..." in this slot
				item ??= GetEmperorNewItemForSlot(ind);
				byte dye = (invItem?.Stain) ?? default;


				EquipItem newItem = new()
				{
					Id = (item?.Model.Id)??0,
					Variant = (byte)((item?.Model.Variant)??0),
					Dye = dye,
				};
				itemsToEquip.Add((ind, newItem));
			}

			return itemsToEquip;
		}
		public static unsafe List<(EquipIndex index, EquipItem equip)> EquipSetGlamourDresser(SetLookup setLookup)
		{
			throw new NotImplementedException();
		}
		public static Item GetEmperorNewItemForSlot(EquipIndex equipIndex) => Items!.Where(i => i.IsEquippable(Equipment.EquipIndexToItemSlot(equipIndex)) && i.Name.Contains("Emperor's New")).First();

	}
	public enum SetSource
	{
		GearSet,
		GlamourDresser,
		Glamaholic,
		Glamourer,
	};

	public class SetLookup {
		public int ID;
		public SetSource Source;
		public string Name;
		public SetLookup(int iD, string name, SetSource source)
		{
			ID = iD;
			Name = name;
			Source = source;
		}
	}

	public class ItemCache {
		public EquipItem EquipItem;
		public Item? Item;
		public TextureWrap? Icon;
	}
}