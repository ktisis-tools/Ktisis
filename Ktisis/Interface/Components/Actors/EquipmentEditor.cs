using System;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using System.Collections.Generic;

using Dalamud.Interface.Internal;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin.Services;

using GLib.Popups;

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

	private readonly PopupList<ItemSheet> _itemSelectPopup;
	
	public EquipmentEditor(
		IDataManager data,
		ITextureProvider tex
	) {
		this._data = data;
		this._tex = tex;
		this._itemSelectPopup = new PopupList<ItemSheet>(
			"##ItemSelectPopup",
			ItemSelectDrawRow
		).WithSearch(ItemSelectSearchPredicate);
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

	private readonly static Vector2 ButtonSize = new(48, 48);

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
		
		this.DrawItemSelectPopup(editor, actor);
	}
	
	// Draw item slot

	private void DrawSlots(IAppearanceManager editor, ActorEntity actor, IEnumerable<EquipIndex> slots) {
		using var _ = ImRaii.Group();
		foreach (var index in slots)
			this.DrawSlot(editor, actor, index);
	}

	private void DrawSlot(IAppearanceManager editor, ActorEntity actor, EquipIndex index) {
		var model = editor.GetEquipIndex(actor, index);
			
		ItemSheet? item;
		lock (this.Items) {
			item = this.Items.Where(row => row.IsEquippable(index.ToEquipSlot()))
				.FirstOrDefault(row => row.Model.Id == model.Id && row.Model.Variant == model.Variant);
		}
		
		// Icon

		this.DrawItemButton(index, item);
		if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
			editor.SetEquipIndexIdVariant(actor, index, 0, 0);
			
		ImGui.SameLine();
		
		// Name + Model input

		using var _group = ImRaii.Group();
			
		ImGui.Text(item?.Name ?? (model.Id == 0 ? "Empty" : "Unknown"));
			
		var values = new int[] { model.Id, model.Variant };
		if (ImGui.InputInt2($"##Input{index}", ref values[0]))
			editor.SetEquipIndexIdVariant(actor, index, (ushort)values[0], (byte)values[1]);
	}
	
	// Draw item selectors
	
	private uint GetFallbackIcon(EquipIndex index) => index switch {
		EquipIndex.Head => 60124,
		EquipIndex.Chest => 60125,
		EquipIndex.Hands => 60129,
		EquipIndex.Legs => 60127,
		EquipIndex.Feet => 60130,
		EquipIndex.Necklace => 60132,
		EquipIndex.Earring => 60133,
		EquipIndex.Bracelet => 60134,
		EquipIndex.RingLeft or EquipIndex.RingRight => 60135,
		_ => 0
	};

	private void DrawItemButton(EquipIndex index, ItemSheet? item) {
		using var _col = ImRaii.PushColor(ImGuiCol.Button, 0);

		IDalamudTextureWrap? icon = null;
		if (item != null)
			icon = this._tex.GetIcon(item.Icon);
		icon ??= this._tex.GetIcon(this.GetFallbackIcon(index));
		
		bool clicked;
		if (icon != null)
			clicked = ImGui.ImageButton(icon.ImGuiHandle, ButtonSize);
		else
			clicked = ImGui.Button(index.ToString(), ButtonSize);
		
		if (clicked)
			this.OpenItemSelectPopup(index);
	}
	
	// Item select popup

	private EquipIndex ItemSelectIndex = 0;

	private List<ItemSheet> ItemSelectList = new();

	private void OpenItemSelectPopup(EquipIndex index) {
		this.ItemSelectIndex = index;
		this.ItemSelectList.Clear();
		lock (this.Items)
			this.ItemSelectList = this.Items.Where(item => item.IsEquippable(index.ToEquipSlot())).ToList();
		this._itemSelectPopup.Open();
	}

	private void DrawItemSelectPopup(IAppearanceManager editor, ActorEntity actor) {
		if (!this._itemSelectPopup.IsOpen) return;

		if (this._itemSelectPopup.Draw(this.ItemSelectList, out var selected) && selected != null)
			editor.SetEquipIndexIdVariant(actor, this.ItemSelectIndex, selected.Model.Id, (byte)selected.Model.Variant);
	}

	private static bool ItemSelectDrawRow(ItemSheet item, bool isFocus) => ImGui.Selectable(item.Name, isFocus);

	private static bool ItemSelectSearchPredicate(ItemSheet item, string query) => item.Name.Contains(query, StringComparison.OrdinalIgnoreCase);
}
