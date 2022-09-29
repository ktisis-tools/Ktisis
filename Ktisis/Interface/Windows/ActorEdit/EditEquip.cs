using System.Linq;
using System.Collections.Generic;

using ImGuiNET;

using Dalamud.Logging;

using Ktisis.GameData;
using Ktisis.GameData.Excel;
using Ktisis.Structs.Actor;
using Ktisis.Interface.Windows.ActorEdit;

namespace Ktisis.Interface.Windows.ActorEdit {
	public class EditEquip {
		public unsafe static Actor* Target => EditActor.Target;

		public static List<Item>? Items;

		public static Dictionary<EquipSlot, ItemCache> Equipped = new();

		// Helper stuff. Will move if there's ever a need for this elsewhere.

		public static Item? FindItem(EquipItem item, EquipSlot slot)
			=> Items?.Find(i => i.IsEquippable(slot) && i.Model.Id == item.Id && i.Model.Variant == item.Variant);

		public static EquipIndex SlotToIndex(EquipSlot slot) => (EquipIndex)(slot - ((int)slot >= 5 ? 3 : 2));

		// UI Code

		public unsafe static void Draw() {
			if (Items == null)
				Items = Sheets.GetSheet<Item>().Where(i => i.IsEquippable()).ToList();

			var tar = EditActor.Target;

			for (var i = 2; i < 13; i++) {
				var slot = (EquipSlot)i;
				if (slot == EquipSlot.Waist) continue;
				DrawSelector(slot);
			}

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
			}

			var item = Equipped[slot];

			var name = item.Item == null ? "Unknown" : item.Item.Name;
			ImGui.Text(name);

			ImGui.PushItemWidth(100);
			var val = new int[2] { equip.Id, equip.Variant };
			if (ImGui.InputInt2($"{slot}", ref val[0])) {
				equip.Id = (ushort)val[0];
				equip.Variant = (byte)val[1];
				tar->Equip(index, equip);
			}
			ImGui.PopItemWidth();
		}
	}

	public class ItemCache {
		public EquipItem EquipItem;
		public Item? Item = null!;
	}
}