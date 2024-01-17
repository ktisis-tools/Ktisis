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
using Ktisis.Editor.Characters;
using Ktisis.Editor.Characters.Data;
using Ktisis.Scene.Entities.Game;

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
	private readonly List<ItemSheet> Items = new();

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
			.GetSheet<ItemSheet>()!
			.Where(item => item.IsEquippable());

		foreach (var chunk in items.Chunk(1000)) {
			lock (this.Items) {
				this.Items.AddRange(chunk);
			}
		}
	}
	
	// Draw

	public void Draw(IAppearanceManager editor, ActorEntity actor) {
		this.FetchData();
		
		var avail = ImGui.GetContentRegionAvail();
		try {
			ImGui.PushItemWidth(avail.X / 3);
			var slots = Enum.GetValues<EquipIndex>();
			this.DrawSlots(editor, actor, slots.Take(5));
			ImGui.SameLine();
			this.DrawSlots(editor, actor, slots.Skip(5));
		} finally {
			ImGui.PopItemWidth();
		}
	}

	private void DrawSlots(IAppearanceManager editor, ActorEntity actor, IEnumerable<EquipIndex> slots) {
		using var _ = ImRaii.Group();
		foreach (var index in slots) {
			var model = editor.GetEquipIndex(actor, index);
			
			ItemSheet? item;
			lock (this.Items) {
				item = this.Items.FirstOrDefault(
					row => {
						return row.IsEquippable(index.ToEquipSlot()) && row.Model.Id == model.Id && row.Model.Variant == model.Variant;
					}
				);
			}
			
			if (item != null) {
				var icon = this._tex.GetIcon(item.Icon);
				if (icon != null)
					ImGui.Image(icon.ImGuiHandle, new Vector2(48, 48));
			} else {
				ImGui.Text("-");
			}
			
			ImGui.SameLine();

			using var _group = ImRaii.Group();

			ImGui.Text($"{item?.Name}");
			var values = new int[] { model.Id, model.Variant };
			if (ImGui.InputInt2($"##Input{index}", ref values[0])) {
				model.Id = (ushort)values[0];
				model.Variant = (byte)values[1];
				editor.SetEquipIndex(actor, index, model);
			}
		}
	}

	private unsafe void DrawSlots(CharacterBase* chara, IEnumerable<(EquipmentModelId, int)> slots) {
		using var _ = ImRaii.Group();

		foreach (var (equip, index) in slots) {
			ItemSheet? item;
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
