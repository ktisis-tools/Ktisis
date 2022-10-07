using System;
using System.Linq;
using System.Numerics;
using System.Collections.Generic;

using ImGuiNET;
using ImGuiScene;

using Dalamud.Logging;

using Ktisis.GameData;
using Ktisis.GameData.Excel;
using Ktisis.Structs.Actor;

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

		// Helper stuff. Will move if there's ever a need for this elsewhere.

		public static Item? FindItem(EquipItem item, EquipSlot slot)
			=> Items?.FirstOrDefault(i => i.IsEquippable(slot) && i.Model.Id == item.Id && i.Model.Variant == item.Variant, null!);

		public static EquipIndex SlotToIndex(EquipSlot slot) => (EquipIndex)(slot - ((int)slot >= 5 ? 3 : 2));

		// UI Code

		public unsafe static void Draw() {
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
	}

	public class ItemCache {
		public EquipItem EquipItem;
		public Item? Item;
		public TextureWrap? Icon;
	}
}