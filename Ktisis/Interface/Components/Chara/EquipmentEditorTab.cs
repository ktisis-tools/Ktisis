using System;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using System.Collections.Generic;

using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using Dalamud.Bindings.ImGui;

using Stain = Lumina.Excel.Sheets.Stain;

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
	private readonly PopupList<Glasses> _glassesSelectPopup;

	private IEquipmentEditor _editor;

	public IEquipmentEditor Editor {
		private get => this._editor;
		set {
			this._editor = value;
			this.InvalidateCache();
		}
	}
	
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

		this._glassesSelectPopup = new PopupList<Glasses>(
			"##GlassesSelectPopup",
			GlassesSelectDrawRow
		).WithSearch(GlassesSelectSearchPredicate);
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
		
		this.DrawGlassesSelect();
		
		this.DrawPopups();
	}

	private void DrawPopups() {
		this.DrawItemSelectPopup();
		this.DrawDyeSelectPopup();
		this.DrawGlassesSelectPopup();
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
			if (ImGui.InputInt($"##Input{slot}", values))
				wep.SetModel((ushort)values[0], (ushort)values[1], (byte)values[2]);
		} else if (info is EquipInfo equip) {
			var values = new int[] { equip.Model.Id, equip.Model.Variant };
			if (ImGui.InputInt($"##Input{slot}", values))
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
		
		ImGui.SetNextItemWidth(CalcItemWidth(cursorStart));
	}
	
	// Draw item selectors

	private void DrawItemButton(ItemInfo info) {
		using var _col = ImRaii.PushColor(ImGuiCol.Button, 0);
		
		bool clicked;
		using (var _ = ImRaii.PushId($"##ItemButton_{info.Slot}")) {
			if (info.Texture != null)
				clicked = ImGui.ImageButton(info.Texture.GetWrapOrEmpty().Handle, ButtonSize);
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

		if (!this._itemSelectPopup.Draw(this.ItemSelectList, out var selected)) return;
		
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
		if (stain != null) color |= (stain.Value.Color << 8).FlipEndian();
		return color;
	}

	private void DrawDyeButton(ItemInfo info, int index) {
		Stain? stain = null;
		foreach (var row in this.Stains) {
			if (row.RowId != info.StainIds[index])
				continue;
			lock (this.Stains)
				stain = row;
		}

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
		var name = stain?.Name.ExtractText();
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
			) info.SetStainId((byte)selected.RowId, this.DyeSelectIndex);
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
		var name = stain.RowId == 0 ? "None" : stain.Name.ExtractText();
		return ImGui.Selectable(name, isFocus);
	}
	
	private static bool DyeSelectSearchPredicate(Stain stain, string query)
		=> stain.Name.ExtractText().Contains(query, StringComparison.OrdinalIgnoreCase);
	
	// Draw glasses slots

	private void DrawGlassesSelect(int index = 0) {
		// Fetch glasses data
		
		var glassesId = this.Editor.GetGlassesId(index);
		
		Glasses? glasses;
		lock (this.Glasses)
			glasses = this.Glasses.FirstOrDefault(x => x.RowId == glassesId);
		
		// Draw button

		var cursorStart = ImGui.GetCursorPosX();
		this.DrawGlassesButton(index, glasses);
		
		ImGui.SameLine(0, ImGui.GetStyle().ItemInnerSpacing.X);
		
		// Draw name + ID input
		
		using var _ = ImRaii.Group();
		
		ImGui.Text(glasses?.RowId is not 0 ? glasses!.Value.Name : "None");
		ImGui.SetNextItemWidth(CalcItemWidth(cursorStart) + (ImGui.GetFrameHeight() + ImGui.GetStyle().ItemInnerSpacing.X) * 2);

		var intGlassesId = (int)glassesId;
		if (ImGui.InputInt($"##Glasses_{index}", ref intGlassesId))
			this.Editor.SetGlassesId(index, (ushort)intGlassesId);
	}

	private void DrawGlassesButton(int index, Glasses? glasses) {
		using var _ = ImRaii.PushColor(ImGuiCol.Button, 0);

		var iconId = glasses?.Icon is not null and not 0 ? glasses.Value.Icon : GetFallbackIcon(EquipSlot.Glasses); 
		var icon = this._tex.GetFromGameIcon(iconId);
		if (ImGui.ImageButton(icon.GetWrapOrEmpty().Handle, ButtonSize))
			this.OpenGlassesSelectPopup(index);
		
		if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
			this.Editor.SetGlassesId(index, 0);
	}
	
	private static bool GlassesSelectSearchPredicate(Glasses glasses, string query)
		=> glasses.Name.Contains(query, StringComparison.OrdinalIgnoreCase);
	
	// Glasses select popup

	private int GlassesSelectIndex = 0;
	
	private void OpenGlassesSelectPopup(int index) {
		this.GlassesSelectIndex = index;
		this._glassesSelectPopup.Open();
	}

	private void DrawGlassesSelectPopup() {
		if (!this._glassesSelectPopup.IsOpen) return;
		lock (this.Glasses) {
			if (this._glassesSelectPopup.Draw(this.Glasses, out var selected))
				this.Editor.SetGlassesId(this.GlassesSelectIndex, (ushort)selected.RowId);
		}
	}
	
	private static bool GlassesSelectDrawRow(Glasses glasses, bool isFocus)
		=> ImGui.Selectable(!glasses.Name.IsNullOrEmpty() ? glasses.Name : "None", isFocus);
	
	// Data

	private bool _itemsRaii;
	
	private readonly List<ItemSheet> Items = new();
	private readonly List<Stain> Stains = new();
	private readonly List<Glasses> Glasses = new();

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
		
		// Dyes

		var dyes = this._data.Excel.GetSheet<Stain>()!
			.Where(stain => stain.RowId == 0 || !stain.Name.IsEmpty);
		
		lock (this.Stains) this.Stains.AddRange(dyes);
		
		// Items
		
		var items = this._data.Excel
			.GetSheet<ItemSheet>()!
			.Where(item => item.IsEquippable());

		foreach (var chunk in items.Chunk(1000)) {
			lock (this.Items) this.Items.AddRange(chunk);
			lock (this._equipUpdateLock) {
				foreach (var (slot, info) in this.Equipped.Where(pair => pair.Value.Item == null)) {
					if (!chunk.Any(item => item.IsEquippable(slot) && info.IsItemPredicate(item)))
						continue;
					info.FlagUpdate = true;
				}
			}
		}
		
		// Glasses

		var glasses = this._data.Excel.GetSheet<Glasses>()
			.Where(x => x.RowId == 0 || !x.Name.IsNullOrEmpty());
		lock (this.Glasses) this.Glasses.AddRange(glasses);
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
				foreach (var row in this.Items) {
					if (!row.IsEquippable(slot) || !item.IsItemPredicate(row)) continue;
					item.Item = row;
					break;
				}
			}
			item.Texture = item.Item != null ? this._tex.GetFromGameIcon((uint)item.Item.Value.Icon) : null;
			item.Texture ??= this._tex.GetFromGameIcon(GetFallbackIcon(slot));
		} finally {
			this.Equipped[slot] = item;
		}
	}

	private void InvalidateCache() {
		lock (this.Equipped)
			this.Equipped.Clear();
	}
	
	// Utility

	private static float CalcItemWidth(float cursorStart) {
		var innerSpace = ImGui.GetStyle().ItemInnerSpacing.X;
		return Math.Min(
			UiBuilder.IconFont.FontSize * 4 * 2 + innerSpace,
			ImGui.CalcItemWidth() - (ImGui.GetCursorPosX() - cursorStart) - innerSpace - ImGui.GetFrameHeight()
		);
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
		EquipSlot.Glasses => 60189,
		_ => 0
	};
}
