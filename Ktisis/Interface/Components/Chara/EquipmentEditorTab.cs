using System;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using System.Collections.Generic;

using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin.Services;
using Dalamud.Utility;

using Lumina.Excel.GeneratedSheets;

using ImGuiNET;

using GLib.Popups;
using GLib.Widgets;

using Ktisis.Common.Extensions;
using Ktisis.Common.Utility;
using Ktisis.Core.Attributes;
using Ktisis.Editor.Characters.State;
using Ktisis.Editor.Characters.Types;
using Ktisis.GameData.Excel;
using Ktisis.Interface.Components.Chara.Types;

namespace Ktisis.Interface.Components.Chara;

[Transient]
public class EquipmentEditorTab {
	private readonly IDataManager _data;
	private readonly ITextureProvider _tex;

	private readonly PopupList<ItemSheet> _itemSelectPopup;
	private readonly PopupList<Stain> _dyeSelectPopup;
	
	public IEquipmentEditor Editor { set; private get; } = null!;
	
	public EquipmentEditorTab(
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
	
	// Draw

	private readonly static EquipSlot[] EquipSlots = Enum.GetValues<EquipIndex>()
		.Select(index => index.ToEquipSlot())
		.ToArray();

	private readonly static Vector2 ButtonSize = new(42, 42);
	public void Draw() {
		this.FetchData();
		
		var style = ImGui.GetStyle();
		var avail = ImGui.GetWindowSize();
		ImGui.PushItemWidth(avail.X / 2 - style.ItemSpacing.X);
		try {
			lock (this._equipUpdateLock) {
				this.DrawItemSlots(EquipSlots.Take(5).Prepend(EquipSlot.MainHand));
				ImGui.SameLine(0, style.ItemSpacing.X);
				this.DrawItemSlots(EquipSlots.Skip(5).Prepend(EquipSlot.OffHand));
			}
		} finally {
			ImGui.PopItemWidth();
		}
		
		this.DrawItemSelectPopup();
		this.DrawDyeSelectPopup();
	}
	
	// Draw item slot

	private void DrawItemSlots(IEnumerable<EquipSlot> slots) {
		using var _ = ImRaii.Group();
		foreach (var slot in slots)
			this.DrawItemSlot(slot);
	}

	private void DrawItemSlot(EquipSlot slot) {
		this.UpdateSlot(slot);
		if (!this.Equipped.TryGetValue(slot, out var info)) return;

		var cursorStart = ImGui.GetCursorPosX();
		var innerSpace = ImGui.GetStyle().ItemInnerSpacing.X;
		
		// Icon
		
		this.DrawItemButton(info);
		
		ImGui.SameLine(0, innerSpace);
		
		// Name + Model input

		using var _group = ImRaii.Group();
		
		PrepareItemLabel(info.Item, info.ModelId, cursorStart, innerSpace);

		if (info is WeaponInfo wep) {
			var values = new int[] { wep.Model.Id, wep.Model.Type, wep.Model.Variant };
			if (ImGui.InputInt3($"##Input{slot}", ref values[0]))
				wep.SetModel((ushort)values[0], (ushort)values[1], (byte)values[2]);
		} else if (info is EquipInfo equip) {
			var values = new int[] { equip.Model.Id, equip.Model.Variant };
			if (ImGui.InputInt2($"##Input{slot}", ref values[0]))
				equip.SetModel((ushort)values[0], (byte)values[1]);
		}
		
		ImGui.SameLine(0, innerSpace);
		this.DrawDyeButton(info, 0);
		ImGui.SameLine(0, innerSpace);
		this.DrawDyeButton(info, 1);

		if (info.IsHideable) {
			using var _id = ImRaii.PushId($"EqSetVisible_{slot}");
			using var _col0 = ImRaii.PushColor(ImGuiCol.Button, 0);
			
			ImGui.SameLine(0, innerSpace);

			var isVisible = info.IsVisible;
			var icon = isVisible ? FontAwesomeIcon.Eye : FontAwesomeIcon.EyeSlash;
			if (Buttons.IconButtonTooltip(icon, "Toggle item visibility"))
				info.SetVisible(!isVisible);
		}

		if (info.IsVisor) {
			using var _id = ImRaii.PushId($"EqSetToggle_{slot}");
			using var _col0 = ImRaii.PushColor(ImGuiCol.Button, 0);
			
			ImGui.SameLine(0, innerSpace);
			
			var isToggled = info.IsVisorToggled;
			
			using var _col1 = ImRaii.PushColor(ImGuiCol.Text, ImGui.GetColorU32(ImGuiCol.TextDisabled), isToggled);
			if (Buttons.IconButtonTooltip(FontAwesomeIcon.Mask, "Toggle visor"))
				info.SetVisorToggled(!isToggled);
		}
	}

	private static void PrepareItemLabel(ItemSheet? item, ushort modelId, float cursorStart, float innerSpace) {
		var labelWidth = ImGui.CalcItemWidth() - (ImGui.GetCursorPosX() - cursorStart);
		ImGui.SetNextItemWidth(labelWidth);
		ImGui.Text((item?.Name ?? (modelId == 0 ? "Empty" : "Unknown")).FitToWidth(labelWidth));
		
		ImGui.SetNextItemWidth(Math.Min(
			UiBuilder.IconFont.FontSize * 4 * 2 + innerSpace,
			ImGui.CalcItemWidth() - (ImGui.GetCursorPosX() - cursorStart) - innerSpace - ImGui.GetFrameHeight()
		));
	}
	
	// Draw item selectors

	private void DrawItemButton(ItemInfo info) {
		using var _col = ImRaii.PushColor(ImGuiCol.Button, 0);
		
		bool clicked;
		using (var _ = ImRaii.PushId($"##ItemButton_{info.Slot}")) {
			if (info.Texture != null)
				clicked = ImGui.ImageButton(info.Texture.GetWrapOrEmpty().ImGuiHandle, ButtonSize);
			else
				clicked = ImGui.Button(info.Slot.ToString(), ButtonSize);
		}

		if (clicked)
			this.OpenItemSelectPopup(info.Slot);
		
		if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
			info.Unequip();
	}
	
	// Item select popup

	private EquipSlot ItemSelectSlot = 0;
	private List<ItemSheet> ItemSelectList = new();

	private void OpenItemSelectPopup(EquipSlot slot) {
		this.ItemSelectSlot = slot;
		this.ItemSelectList.Clear();
		lock (this.Items)
			this.ItemSelectList = this.Items.Where(item => item.IsEquippable(slot)).ToList();
		this._itemSelectPopup.Open();
	}

	private void DrawItemSelectPopup() {
		if (!this._itemSelectPopup.IsOpen) return;

		if (!this._itemSelectPopup.Draw(this.ItemSelectList, out var selected) || selected == null) return;

		lock (this.Equipped) {
			if (this.Equipped.TryGetValue(this.ItemSelectSlot, out var info))
				info.SetEquipItem(selected);
		}
	}

	private static bool ItemSelectDrawRow(ItemSheet item, bool isFocus) => ImGui.Selectable(item.Name, isFocus);
	private static bool ItemSelectSearchPredicate(ItemSheet item, string query) => item.Name.Contains(query, StringComparison.OrdinalIgnoreCase);
	
	// Draw dye selector

	private static uint CalcStainColor(Stain? stain) {
		var color = 0xFF000000u;
		if (stain != null) color |= (stain.Color << 8).FlipEndian();
		return color;
	}

	private void DrawDyeButton(ItemInfo info, int index) {
		Stain? stain;
		lock (this.Stains)
			stain = this.Stains.FirstOrDefault(row => row.RowId == info.StainIds[index]);

		var color = CalcStainColor(stain);
		var colorVec4 = ImGui.ColorConvertU32ToFloat4(color);
		if (ImGui.ColorButton($"##DyeSelect_{info.Slot}_{index}", colorVec4, ImGuiColorEditFlags.NoTooltip))
			this.OpenDyeSelectPopup(info.Slot, index);

		if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
			info.SetStainId(0, index);

		if (ImGui.IsItemHovered()) DrawDyeTooltip(stain, color, colorVec4);
	}

	private static void DrawDyeTooltip(Stain? stain, uint color, Vector4 colorVec4) {
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
	
	private EquipSlot DyeSelectSlot = 0;
	private int DyeSelectIndex = 0;

	private void OpenDyeSelectPopup(EquipSlot slot, int index) {
		this.DyeSelectSlot = slot;
		this.DyeSelectIndex = index;
		this._dyeSelectPopup.Open();
	}

	private void DrawDyeSelectPopup() {
		if (!this._dyeSelectPopup.IsOpen) return;
		lock (this.Stains) {
			if (
				this._dyeSelectPopup.Draw(this.Stains, out var selected)
				&& this.Equipped.TryGetValue(this.DyeSelectSlot, out var info)
			) info.SetStainId((byte)selected!.RowId, this.DyeSelectIndex);
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
	
	// Data

	private bool _itemsRaii;
	
	private readonly List<ItemSheet> Items = new();
	private readonly List<Stain> Stains = new();

	private readonly object _equipUpdateLock = new();
	private readonly Dictionary<EquipSlot, ItemInfo> Equipped = new();

	private void FetchData() {
		if (this._itemsRaii) return;
		this._itemsRaii = true;
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

		foreach (var chunk in items.Chunk(1000)) {
			lock (this.Items) this.Items.AddRange(chunk);
			lock (this._equipUpdateLock) {
				foreach (var (_, info) in this.Equipped.Where(pair => pair.Value.Item == null))
					info.FlagUpdate = true;
			}
		}
	}

	private void UpdateSlot(EquipSlot slot) {
		if (this.Equipped.TryGetValue(slot, out var info) && !info.FlagUpdate && info.IsCurrent()) return;
		
		ItemInfo item;

		var isWeapon = slot < EquipSlot.Head;
		if (isWeapon) {
			var index = (WeaponIndex)slot;
			var model = this.Editor.GetWeaponIndex(index);
			item = new WeaponInfo(this.Editor) {
				Index = index,
				Model = model
			};
		} else {
			var index = slot.ToEquipIndex();
			var model = this.Editor.GetEquipIndex(index);
			item = new EquipInfo(this.Editor) {
				Index = index,
				Model = model
			};
		}

		try {
			lock (this.Items) {
				item.Item = this.Items
					.Where(row => row.IsEquippable(slot))
					.FirstOrDefault(item.IsItemPredicate);
			}
			item.Texture = item.Item != null ? this._tex.GetFromGameIcon((uint)item.Item.Icon) : null;
			item.Texture ??= this._tex.GetFromGameIcon(GetFallbackIcon(slot));
		} finally {
			this.Equipped[slot] = item;
		}
	}
	
	private static uint GetFallbackIcon(EquipSlot slot) => slot switch {
		EquipSlot.MainHand => 60102,
		EquipSlot.OffHand => 60110,
		EquipSlot.Head => 60124,
		EquipSlot.Chest => 60125,
		EquipSlot.Hands => 60129,
		EquipSlot.Legs => 60127,
		EquipSlot.Feet => 60130,
		EquipSlot.Necklace => 60132,
		EquipSlot.Earring => 60133,
		EquipSlot.Bracelet => 60134,
		EquipSlot.RingLeft or EquipSlot.RingRight => 60135,
		_ => 0
	};
}
