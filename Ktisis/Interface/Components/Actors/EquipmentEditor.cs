using System;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using System.Collections.Generic;

using Dalamud.Interface;
using Dalamud.Interface.Internal;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin.Services;
using Dalamud.Utility;

using Lumina.Excel.GeneratedSheets;

using GLib.Popups;

using ImGuiNET;

using Ktisis.Common.Extensions;
using Ktisis.Common.Utility;
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
	private readonly PopupList<Stain> _dyeSelectPopup;
	
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

		this._dyeSelectPopup = new PopupList<Stain>(
			"##DyeSelectPopup",
			DyeSelectDrawRow
		).WithSearch(DyeSelectSearchPredicate);
	}
	
	// Data

	private bool ItemsFetched;
	private readonly List<ItemSheet> Items = new();
	private readonly List<Stain> Stains = new();

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

		var dyes = this._data.Excel.GetSheet<Stain>()!
			.Where(stain => stain.RowId == 0 || !stain.Name.RawString.IsNullOrEmpty());
		
		lock (this.Stains) this.Stains.AddRange(dyes);

		foreach (var chunk in items.Chunk(1000))
			lock (this.Items) this.Items.AddRange(chunk);
	}
	
	// Draw

	private readonly static Vector2 ButtonSize = new(48, 48);

	public void Draw(IAppearanceManager editor, ActorEntity actor) {
		this.FetchData();
		
		var style = ImGui.GetStyle();
		var avail = ImGui.GetWindowSize();
		ImGui.PushItemWidth(avail.X / 2 - style.ItemSpacing.X);
		try {
			var slots = Enum.GetValues<EquipIndex>();
			this.DrawSlots(editor, actor, slots.Take(5));
			ImGui.SameLine(0, style.ItemSpacing.X);
			this.DrawSlots(editor, actor, slots.Skip(5));
		} finally {
			ImGui.PopItemWidth();
		}
		
		this.DrawItemSelectPopup(editor, actor);
		this.DrawDyeSelectPopup(editor, actor);
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

		var cursorStart = ImGui.GetCursorPosX();

		var innerSpace = ImGui.GetStyle().ItemInnerSpacing.X;
		
		// Icon

		this.DrawItemButton(index, item);
		if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
			editor.SetEquipIndexIdVariant(actor, index, 0, 0);
			
		ImGui.SameLine(0, innerSpace);
		
		// Name + Model input

		using var _group = ImRaii.Group();
		
		var labelWidth = ImGui.CalcItemWidth() - (ImGui.GetCursorPosX() - cursorStart);
		ImGui.SetNextItemWidth(labelWidth);
		ImGui.Text((item?.Name ?? (model.Id == 0 ? "Empty" : "Unknown")).FitToWidth(labelWidth));
		
		ImGui.SetNextItemWidth(Math.Min(
			UiBuilder.IconFont.FontSize * 4 * 2 + innerSpace,
			ImGui.CalcItemWidth() - (ImGui.GetCursorPosX() - cursorStart) - innerSpace - ImGui.GetFrameHeight()
		));
		
		var values = new int[] { model.Id, model.Variant };
		if (ImGui.InputInt2($"##Input{index}", ref values[0]))
			editor.SetEquipIndexIdVariant(actor, index, (ushort)values[0], (byte)values[1]);
		
		ImGui.SameLine(0, innerSpace);
		this.DrawDyeButton(index, model.Stain);
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
		if (this._itemSelectPopup.IsOpen && this._itemSelectPopup.Draw(this.ItemSelectList, out var selected) && selected != null)
			editor.SetEquipIndexIdVariant(actor, this.ItemSelectIndex, selected.Model.Id, (byte)selected.Model.Variant);
	}

	private static bool ItemSelectDrawRow(ItemSheet item, bool isFocus) => ImGui.Selectable(item.Name, isFocus);
	private static bool ItemSelectSearchPredicate(ItemSheet item, string query) => item.Name.Contains(query, StringComparison.OrdinalIgnoreCase);
	
	// Draw dye selector

	private static uint CalcStainColor(Stain? stain) {
		var color = 0xFF000000u;
		if (stain != null) color |= (stain.Color << 8).FlipEndian();
		return color;
	}

	private void DrawDyeButton(EquipIndex index, byte stainId) {
		Stain? stain;
		lock (this.Stains)
			stain = this.Stains.FirstOrDefault(row => row.RowId == stainId);

		var color = CalcStainColor(stain);
		var colorVec4 = ImGui.ColorConvertU32ToFloat4(color);
		if (ImGui.ColorButton($"##DyeSelect_{index}", colorVec4, ImGuiColorEditFlags.NoTooltip))
			this.OpenDyeSelectPopup(index);

		if (ImGui.IsItemHovered()) this.DrawDyeTooltip(stain, color, colorVec4);
	}

	private void DrawDyeTooltip(Stain? stain, uint color, Vector4 colorVec4) {
		using var _color = ImRaii.PushColor(ImGuiCol.Text, color, (colorVec4.X + colorVec4.Y + colorVec4.Z) / 3 > 0.10f);
		using var _tooltip = ImRaii.Tooltip();
		// Text
		var name = stain?.Name?.RawString;
		ImGui.Text(!name.IsNullOrEmpty() ? name : "No dye set.");
		// RGB Hex
		var col = stain?.Color ?? 0;
		if (col == 0) return;
		ImGui.SameLine(0, ImGui.GetStyle().ItemInnerSpacing.X);
		ImGui.TextDisabled($"(#{col:X6})");
	}
	
	// Dye select popup
	
	private EquipIndex DyeSelectIndex = 0;

	private void OpenDyeSelectPopup(EquipIndex index) {
		this.DyeSelectIndex = index;
		this._dyeSelectPopup.Open();
	}

	private void DrawDyeSelectPopup(IAppearanceManager editor, ActorEntity actor) {
		if (!this._dyeSelectPopup.IsOpen) return;
		lock (this.Stains) {
			if (this._dyeSelectPopup.Draw(this.Stains, out var selected) && selected != null)
				editor.SetEquipIndexStainId(actor, this.DyeSelectIndex, (byte)selected.RowId);
		}
	}

	private static bool DyeSelectDrawRow(Stain stain, bool isFocus) {
		var color = CalcStainColor(stain);

		var style = ImGui.GetStyle();
		var space = style.ItemSpacing.Y / 2;
		var bg = ImGui.GetWindowDrawList();
		var min = ImGui.GetCursorScreenPos();
		min.X -= style.WindowPadding.X + space;
		var max = min + ImGui.GetContentRegionAvail() with { Y = UiBuilder.IconFont.FontSize + style.FramePadding.Y + space };
		bg.AddRectFilled(min, max, color);

		using var _textCol = ImRaii.PushColor(ImGuiCol.Text, GuiHelpers.CalcBlackWhiteTextColor(color));
		using var _activeCol = ImRaii.PushColor(ImGuiCol.HeaderActive, color);
		using var _hoverCol = ImRaii.PushColor(ImGuiCol.HeaderHovered, color);
		var name = stain.RowId == 0 ? "None" : stain.Name;
		return ImGui.Selectable(name, isFocus);
	}
	
	private static bool DyeSelectSearchPredicate(Stain stain, string query)
		=> stain.Name.RawString.Contains(query, StringComparison.OrdinalIgnoreCase);
}
