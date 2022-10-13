using System;
using System.Linq;
using System.Numerics;
using System.Collections.Generic;

using ImGuiNET;
using ImGuiScene;

using Ktisis.GameData;
using Ktisis.GameData.Excel;
using Ktisis.Structs.Actor;
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

		public static EquipSlot? SlotSelect;
		public static IEnumerable<Item>? SlotItems;
		public static string ItemSearch = "";
		public static string SetSearch = "";
		public static bool DrawSetSelection = false;
		public static EquipmentSets? Sets = null;

		// Helper stuff. Will move if there's ever a need for this elsewhere.

		public static Item? FindItem(EquipItem item, EquipSlot slot)
			=> Items?.FirstOrDefault(i => i.IsEquippable(slot) && i.Model.Id == item.Id && i.Model.Variant == item.Variant, null!);

		public static EquipIndex SlotToIndex(EquipSlot slot) => (EquipIndex)(slot - ((int)slot >= 5 ? 3 : 2));

		// UI Code

		public unsafe static void Draw() {

			if (Items == null)
				Items = Sheets.GetSheet<Item>().Where(i => i.IsEquippable());

			DrawControls();

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
		}

		public static void OpenSetSelector() {
			DrawSetSelection = true;
		}

		public static void DrawControls()
		{
			Sets = new EquipmentSets(Items!);

			if (GuiHelpers.IconButtonTooltip(FontAwesomeIcon.Tshirt, "Look up for a set."))
				OpenSetSelector();

			if (DrawSetSelection)
				DrawSetSelectorList();

			ImGui.Separator();
		}

		public unsafe static void DrawSelectorList(EquipIndex index, EquipItem equip)
		{
			if (SlotItems == null)
				return;

			GuiHelpers.HoverPopupWindow(
					GuiHelpers.HoverPopupWindowFlags.SelectorList | GuiHelpers.HoverPopupWindowFlags.SearchBar,
					SlotItems,
					(i) => false, // draw Before Line
					(i) => i.Name, // lineLabel
					(i) => false, // draw After Line
					(i) => { // on Select
						equip.Id = i.Model.Id;
						equip.Variant = (byte)i.Model.Variant;
						Target->Equip(index, equip);
					},
					() => { // on close
						SlotSelect = null;
						SlotItems = null;
						//ItemSearch = ""; // to forget the search input on close
					},
					ref ItemSearch,
					"Item Select",
					"##equip_items",
					"##equip_search"
					);
		}

		public unsafe static void DrawSetSelectorList()
		{
			if (Sets?.LoadSources() == null)
				Sets = EquipmentSets.InitAndLoadSources(Items!);

			IEnumerable<EquipmentSet> sets = Sets.GetSets();

			if (!sets.Any())
				return;

			GuiHelpers.HoverPopupWindow(
					GuiHelpers.HoverPopupWindowFlags.SelectorList | GuiHelpers.HoverPopupWindowFlags.SearchBar,
					sets.Cast<dynamic>(),
					(i) => false, // draw Before Line
					(i) => i.Name, // lineLabel
					(i) => { // draw After Line
						ImGui.SameLine();
						ImGui.PushStyleVar(ImGuiStyleVar.Alpha, 0.5f);
						GuiHelpers.TextCentered($"{i.Source}");
						ImGui.PopStyleVar();

						return false; // return true to select
					},
					(i) => Target->Equip(Sets.GetItems(i)), // on Select
					() => DrawSetSelection = false, // on close
					ref SetSearch,
					"Set Select",
					"##equip_sets",
					"##set_search"
					);
		}

	}

	public class ItemCache {
		public EquipItem EquipItem;
		public Item? Item;
		public TextureWrap? Icon;
	}
}