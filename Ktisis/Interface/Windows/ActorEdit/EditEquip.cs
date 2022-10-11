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

		public const ImGuiKey KeyBindBrowseUp = ImGuiKey.UpArrow;
		public const ImGuiKey KeyBindBrowseDown = ImGuiKey.DownArrow;
		public const ImGuiKey KeyBindBrowseUpFast = ImGuiKey.PageUp;
		public const ImGuiKey KeyBindBrowseDownFast = ImGuiKey.PageDown;
		public const int FastScrollLineJump = 8; // number of lines on the screen?

		// Properties

		public unsafe static Actor* Target => EditActor.Target;

		public static IEnumerable<Item>? Items;

		public static Dictionary<EquipSlot, ItemCache> Equipped = new();

		public static Vector2 SelectPos;
		public static EquipSlot? SlotSelect;
		public static IEnumerable<Item>? SlotItems;
		public static string ItemSearch = "";
		public static string SetSearch = "";
		public static int LastSelectedItemKey = 0;
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
			SelectPos = ImGui.GetMousePos();
		}

		public static void OpenSetSelector() {
			DrawSetSelection = true;
			SelectPos = ImGui.GetMousePos();
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

			if (BeginHoverPopupWindow(HoverPopupWindowFlags.SelectorList | HoverPopupWindowFlags.SearchBar, ref ItemSearch, "Item Select", "##equip_items"))
			{
				var items = SlotItems;
				if (ItemSearch.Length > 0)
					items = items.Where(i => i.Name.Contains(ItemSearch, StringComparison.OrdinalIgnoreCase));

				LoopHoverPopupWindow(
					HoverPopupWindowFlags.SelectorList,
					items,
					(i) => { // draw Before Line

						return false; // return true to select
					},
					(i) => { // draw After Line

						return false; // return true to select
					},
					(i) => { // on Select
						equip.Id = i.Model.Id;
						equip.Variant = (byte)i.Model.Variant;
						Target->Equip(index, equip);

					},
					null);
			}
			EndHoverPopupWindow(
				HoverPopupWindowFlags.SelectorList,
				() => { // on close
					SlotSelect = null;
					SlotItems = null;
				});
		}

		public unsafe static void DrawSetSelectorList()
		{
			if (Sets?.LoadSources() == null)
				Sets = EquipmentSets.InitAndLoadSources(Items!);

			IEnumerable<EquipmentSet> sets = Sets.GetSets();

			if (!sets.Any())
				return;


			if (BeginHoverPopupWindow(HoverPopupWindowFlags.SelectorList | HoverPopupWindowFlags.SearchBar, ref SetSearch, "Set Select", "##equip_sets"))
			{
				if (SetSearch.Length > 0)
					sets = sets.Where(s => s.Name.Contains(SetSearch, StringComparison.OrdinalIgnoreCase));

				LoopHoverPopupWindow(
					HoverPopupWindowFlags.SelectorList,
					sets.Cast<dynamic>(),
					(i) => { // draw Before Line

						return false; // return true to select
					},
					(i) => { // draw After Line
						ImGui.SameLine();
						ImGui.PushStyleVar(ImGuiStyleVar.Alpha, 0.5f);
						GuiHelpers.TextCentered($"{i.Source}");
						ImGui.PopStyleVar();

						return false; // return true to select
					},
					(i) => { // on Select
						Target->Equip(Sets.GetItems(i));

					},
					null);
			}
			EndHoverPopupWindow(
				HoverPopupWindowFlags.SelectorList,
				() => { // on close
					DrawSetSelection = false;
				});
		}


		// HoverPopupWindow properties and methods
		private static bool HoverPopupWindowIsBegan = false;
		private static bool HoverPopupWindowFocus = false;
		private static bool HoverPopupWindowSearchBarValidated = false;
		public enum HoverPopupWindowFlags
		{
			None,
			SelectorList,
			SearchBar,
		}

		private static bool BeginHoverPopupWindow(HoverPopupWindowFlags flags, ref string InputSearch, string windowLabel = "",string listLabel = "")
		{
			var size = new Vector2(-1, -1);
			ImGui.SetNextWindowSize(size, ImGuiCond.Always);

			ImGui.SetNextWindowPos(SelectPos);
			ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(10, 10));

			HoverPopupWindowIsBegan = ImGui.Begin(windowLabel, ImGuiWindowFlags.NoDecoration);
			if (HoverPopupWindowIsBegan)
			{

				HoverPopupWindowFocus = ImGui.IsWindowFocused() || ImGui.IsWindowHovered();
				ImGui.PushItemWidth(400);
				if(flags.HasFlag(HoverPopupWindowFlags.SearchBar))
					HoverPopupWindowSearchBarValidated = ImGui.InputTextWithHint("##equip_search", "Search...", ref InputSearch, 32, ImGuiInputTextFlags.EnterReturnsTrue);

				if (ImGui.IsWindowFocused(ImGuiFocusedFlags.RootAndChildWindows) && !ImGui.IsAnyItemActive() && !ImGui.IsMouseClicked(ImGuiMouseButton.Left))
					ImGui.SetKeyboardFocusHere(flags.HasFlag(HoverPopupWindowFlags.SearchBar)?-1:0); // TODO: verify the keyboarf focus behaviour when searchbar is disabled

				if (flags.HasFlag(HoverPopupWindowFlags.SelectorList))
					ImGui.BeginListBox(listLabel, new Vector2(-1, 300));

			}
			return HoverPopupWindowIsBegan;
		}
		private static void EndHoverPopupWindow( HoverPopupWindowFlags flags, Action onClose)
		{
			if (HoverPopupWindowIsBegan) {
				if (flags.HasFlag(HoverPopupWindowFlags.SelectorList))
					ImGui.EndListBox();
				HoverPopupWindowFocus |= ImGui.IsItemActive();
				ImGui.PopItemWidth();

				if (!HoverPopupWindowFocus || ImGui.IsKeyPressed(ImGuiKey.Escape))
					onClose();
			}

			ImGui.End();
		}
		private static void LoopHoverPopupWindow(HoverPopupWindowFlags flags, IEnumerable<dynamic> enumerable, Func<dynamic, bool> drawBeforeLine, Func<dynamic, bool> drawAfterLine, Action<dynamic> onSelect, string? lineLabel = null)
		{
			if (!HoverPopupWindowIsBegan) return;

			int indexKey = 0;
			bool isOneSelected = false; // allows one selection per foreach
			if (LastSelectedItemKey >= enumerable.Count()) LastSelectedItemKey = enumerable.Count() - 1;

			foreach (var i in enumerable)
			{
				bool selecting = false;

				selecting |= drawBeforeLine(i);
				if(flags.HasFlag(HoverPopupWindowFlags.SelectorList))
					selecting |= ImGui.Selectable(lineLabel ?? $"{i.Name}", indexKey == LastSelectedItemKey);
				HoverPopupWindowFocus |= ImGui.IsItemFocused();
				selecting |= drawAfterLine(i);

				if (!isOneSelected)
				{
					selecting |= ImGui.IsKeyPressed(KeyBindBrowseUp) && indexKey == LastSelectedItemKey - 1;
					selecting |= ImGui.IsKeyPressed(KeyBindBrowseDown) && indexKey == LastSelectedItemKey + 1;
					selecting |= ImGui.IsKeyPressed(KeyBindBrowseUpFast) && indexKey == LastSelectedItemKey - FastScrollLineJump;
					selecting |= ImGui.IsKeyPressed(KeyBindBrowseDownFast) && indexKey == LastSelectedItemKey + FastScrollLineJump;
					selecting |= HoverPopupWindowSearchBarValidated;
				}

				if (selecting)
				{
					if (ImGui.IsKeyPressed(KeyBindBrowseUp) || ImGui.IsKeyPressed(KeyBindBrowseDown) || ImGui.IsKeyPressed(KeyBindBrowseUpFast) || ImGui.IsKeyPressed(KeyBindBrowseDownFast))
						ImGui.SetScrollY(ImGui.GetCursorPosY() - (ImGui.GetWindowHeight() / 2));

					onSelect(i);
					LastSelectedItemKey = indexKey;
					isOneSelected = true;
				}
				HoverPopupWindowFocus |= ImGui.IsItemFocused();
				indexKey++;
			}
		}


	}

	public class ItemCache {
		public EquipItem EquipItem;
		public Item? Item;
		public TextureWrap? Icon;
	}
}