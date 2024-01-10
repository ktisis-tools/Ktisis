using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin.Services;

using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;

using ImGuiNET;

using Ktisis.Core.Attributes;
using Ktisis.Data.Excel;

namespace Ktisis.Interface.Components.Actors;

[Transient]
public class EquipmentEditor {
	private readonly IDataManager _data;
	private readonly ITextureProvider _tex;
	
	public EquipmentEditor(
		IDataManager data,
		ITextureProvider tex
	) {
		this._data = data;
		this._tex = tex;
	}
	
	// Data

	private bool ItemsFetched;
	private readonly List<ItemRow> Items = new();

	private void FetchData() {
		if (this.ItemsFetched) return;
		this.ItemsFetched = true;

		this.LoadItems().ContinueWith(task => {
			if (task.Exception != null)
				Ktisis.Log.Error($"Failed to fetch items:\n{task.Exception}");
		});
	}

	private async Task LoadItems() {
		await Task.Yield();

		var items = this._data.Excel
			.GetSheet<ItemRow>()!
			.Where(item => item.IsEquippable());

		foreach (var chunk in items.Chunk(1000)) {
			lock (this.Items) {
				this.Items.AddRange(chunk);
			}
		}
	}
	
	// Draw

	public unsafe void Draw(CharacterBase* chara, EquipmentModelId[] equipment) {
		this.FetchData();
		
		var slots = equipment.Select((v, i) => (equip: v, slot: i)).ToList();

		var avail = ImGui.GetContentRegionAvail();
		try {
			ImGui.PushItemWidth(avail.X / 3);
			this.DrawSlots(chara, slots.Take(5));
			var max = ImGui.GetItemRectSize().X + ImGui.GetStyle().ItemSpacing.X * 2;
			ImGui.SameLine(Math.Max(max, avail.X / 2), 0);
			this.DrawSlots(chara, slots.Skip(5));
		} finally {
			ImGui.PopItemWidth();
		}
	}

	private unsafe void DrawSlots(CharacterBase* chara, IEnumerable<(EquipmentModelId, int)> slots) {
		using var _ = ImRaii.Group();

		foreach (var (equip, index) in slots) {
			ItemRow? item;
			lock (this.Items) {
				item = this.Items.FirstOrDefault(
					row => {
						var slot = index + 2;
						if (slot > 4) slot++;
						return row.IsEquippable((EquipSlot)slot) && row.Model.Id == equip.Id && row.Model.Variant == equip.Variant;
					}
				);
			}

			if (item != null) {
				var icon = this._tex.GetIcon(item.Icon);
				if (icon != null)
					ImGui.Image(icon.ImGuiHandle, new Vector2(48, 48));
			} else {
				ImGui.Text(":3");
			}
			
			ImGui.SameLine();

			using var _group = ImRaii.Group();

			ImGui.Text($"{item?.Name}");
			var values = new int[] { equip.Id, equip.Variant };
			if (ImGui.InputInt2($"##Input{index}", ref values[0])) {
				var newEquip = new EquipmentModelId { Id = (ushort)values[0], Variant = (byte)values[1] };
				chara->FlagSlotForUpdate((uint)index, &newEquip);
			}
		}
	}
}
