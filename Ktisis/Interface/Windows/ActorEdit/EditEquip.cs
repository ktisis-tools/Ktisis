using System;
using System.Linq;
using System.Numerics;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using ImGuiNET;

using Dalamud.Interface;
using Dalamud.Interface.Textures;

using Ktisis.Util;
using Ktisis.Data;
using Ktisis.Data.Excel;
using Ktisis.Helpers.Async;
using Ktisis.Structs.Actor;
using Ktisis.Structs.Actor.Equip;
using Ktisis.Interface.Components;
using Ktisis.Structs.Actor.Types;

using Lumina.Excel;

namespace Ktisis.Interface.Windows.ActorEdit {
	public static class EditEquip {
		// Constants

		public static Vector2 IconSize => new(2 * ImGui.GetTextLineHeight() + ImGui.GetStyle().ItemSpacing.Y);

		// Properties

		public unsafe static Actor* Target => EditActor.Target;

		public static IEnumerable<Item>? Items => ItemData.Get();

		public static ExcelSheet<Glasses>? Glasses;

		public static Dictionary<EquipSlot, ItemCache> Equipped = new();

		public static EquipSlot? SlotSelect;
		public static IEnumerable<Item>? SlotItems;
		private static string GlassesSearch = "";
		private static string ItemSearch = "";
		private static string SetSearch = "";
		private static string DyeSearch = "";
		private static bool DrawSetSelection = false;
		private static bool DrawSetDyeSelection = false;
		private static bool DrawGlassesSelection = false;

		private static EquipSlot? SlotSelectDye;
		private static int SelectDyeIndex;
		
		public static IEnumerable<Dye>? Dyes => DyeData.Get();

		public static Item? FindItem(object item, EquipSlot slot)
			=> Items?.FirstOrDefault(i => (item is WeaponEquip ? i.IsWeapon() : i.IsEquippable(slot)) && i.IsEquipItem(item));

		public static EquipIndex SlotToIndex(EquipSlot slot) => (EquipIndex)(slot - ((int)slot >= 5 ? 3 : 2));

		// Async

		private readonly static AsyncData<IEnumerable<Item>> ItemData = new(GetItemData);
		private static IEnumerable<Item> GetItemData(object[] args)
			=> Sheets.GetSheet<Item>().Where(i => i.IsEquippable()).ToList();

		private readonly static AsyncData<IEnumerable<Dye>> DyeData = new(GetDyeData);
		private static IEnumerable<Dye> GetDyeData(object[] args)
			=> Sheets.GetSheet<Dye>()
				.Where(i => i.IsValid())
				.OrderBy(i => i.Shade).ThenBy(i => i.SubOrder)
				.ToList();
		
		// UI Code

		public static void Draw() {
			if (Items == null || Dyes == null) return;
			
			DrawControls();
			
			ImGui.Spacing();
			DrawFaceWear();
			ImGui.Spacing();
			ImGui.Spacing();

			ImGui.BeginGroup();
			for (var i = 2; i < 13; i++) {
				var slot = (EquipSlot)i;
				if (slot == EquipSlot.Waist) continue;
				if (i == 8) {
					ImGui.EndGroup();
					ImGui.SameLine();
					ImGui.BeginGroup();
					DrawSelector(EquipSlot.OffHand);
				}
				if (i == 2) DrawSelector(EquipSlot.MainHand);
				DrawSelector(slot);
			}
			ImGui.EndGroup();
			
			ImGui.EndTabItem();
		}

		public unsafe static void DrawFaceWear() {
			Glasses ??= Services.DataManager.GetExcelSheet<Glasses>()!;
			
			var glassesId = EditActor.Target->DrawData.Glasses;
			var glasses = Glasses.GetRow(glassesId);
			var name = glasses.Name;
			if (ImGui.BeginCombo("Glasses", name)) {
				DrawGlassesSelection = true;
				ImGui.CloseCurrentPopup();
				ImGui.EndCombo();
			}

			if (DrawGlassesSelection)
				DrawGlassesSelector();
		}

		public unsafe static void DrawSelector(EquipSlot slot) {
			var tar = EditActor.Target;
			var isWeapon = slot == EquipSlot.MainHand || slot == EquipSlot.OffHand;
			
			object equipObj;
			if (isWeapon)
				equipObj = tar->GetWeaponEquip(slot);
			else
				equipObj = tar->GetEquip(SlotToIndex(slot));

			var isEmpty = true;
			{
				if (equipObj is ItemEquip equip)
					isEmpty = equip.Id == 0;
				else if (equipObj is WeaponEquip wep)
					isEmpty = wep.Set == 0;
			}

			if (!Equipped.ContainsKey(slot))
				Equipped.Add(slot, new ItemCache(equipObj, slot));
			else if (!Equipped[slot].Equip!.Equals(equipObj))
				Equipped[slot].SetEquip(equipObj, slot);

			var item = Equipped[slot];
			var icon = item.Icon?.GetWrapOrEmpty().ImGuiHandle ?? 0;
			ImGui.PushID((int)slot);
			if (ImGui.ImageButton(icon, IconSize) && SlotSelect == null)
				OpenSelector(slot);
			ImGui.PopID();

			if (ImGui.IsItemClicked(ImGuiMouseButton.Right)) {
				if (isWeapon)
					tar->Equip((int)slot, new WeaponEquip() { Dye = ((WeaponEquip)equipObj).Dye });
				else
					tar->Equip(SlotToIndex(slot), new ItemEquip() { Dye = ((ItemEquip)equipObj).Dye });
			}

			ImGui.SameLine();
			ImGui.BeginGroup();
			
			var name = item.Item?.Name ?? (isEmpty ? "Empty" : "Unknown");
			ImGui.Text(name);

			ImGui.PushItemWidth(120);
			if (isWeapon) {
				var equip = (WeaponEquip)equipObj;
				//PluginLog.Information($"{equip.Set} {equip.Base} {equip.Variant}");
				var val = new int[] { equip.Set, equip.Base, equip.Variant };
				if (ImGui.InputInt3($"##KtisisWep_{slot}", ref val[0])) {
					equip.Set = (ushort)val[0];
					equip.Base = (ushort)val[1];
					equip.Variant = (ushort)val[2];
					tar->Equip((int)slot, equip);
				}
			} else {
				var equip = (ItemEquip)equipObj;
				var val = new int[] { equip.Id, equip.Variant };
				if (ImGui.InputInt2($"##{slot}", ref val[0])) {
					equip.Id = (ushort)val[0];
					equip.Variant = (byte)val[1];
					tar->Equip(SlotToIndex(slot), equip);
				}
			}
			ImGui.PopItemWidth();
			ImGui.SameLine();

			DrawDyeButton((IEquipItem)equipObj, slot, 0);
			ImGui.SameLine();
			DrawDyeButton((IEquipItem)equipObj, slot, 1);

			if (equipObj is WeaponEquip) {
				ImGui.SameLine();

				var wep = slot == EquipSlot.MainHand ? &tar->DrawData.MainHand : &tar->DrawData.OffHand;
				
				var hidden = (wep->Flags & WeaponFlags.Hidden) != 0;
				if (DrawVisibilityToggle($"wep_vis_{slot}", hidden))
					wep->Flags ^= WeaponFlags.Hidden;
			} else if (slot == EquipSlot.Head) {
				ImGui.SameLine();
				
				var hidden = tar->IsHatHidden;
				if (DrawVisibilityToggle("head_vis", hidden)) {
					tar->IsHatHidden ^= true;
					tar->Equip(SlotToIndex(slot), tar->DrawData.Equipment.Head);
				}
			}

			ImGui.EndGroup();

			if (SlotSelect == slot)
				DrawSelectorList(slot, equipObj);
			if (SlotSelectDye == slot)
				DrawDyePicker(slot, equipObj, SelectDyeIndex);
		}

		public static void OpenSelector(EquipSlot slot) {
			SlotSelect = slot;
			SlotItems = Items!.Where(i => i.IsEquippable(slot));
		}
		public static void CloseSelector() {
			SlotSelect = null;
			SlotItems = null;
		}

		private static void DrawDyeButton(IEquipItem item, EquipSlot slot, int index) {
			var dye = Dyes!.FirstOrDefault(i => i.RowId == item.GetDye(index));
			if (ImGui.ColorButton($"{dye.Name} [{dye.RowId}]##{slot}_{index}", dye.ColorVector4, ImGuiColorEditFlags.NoBorder))
				OpenDyePicker(slot, index);
			if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
				SetDye(item, slot, index, 0);
		}

		private unsafe static void SetDye(IEquipItem item, EquipSlot slot, int index, byte value) {
			item.SetDye(index, value);
			if (item is WeaponEquip wepEquip)
				Target->Equip((int)slot, wepEquip);
			else if (item is ItemEquip itemEquip)
				Target->Equip(SlotToIndex(slot), itemEquip);
		}

		public static void OpenDyePicker(EquipSlot slot, int index) {
			SlotSelectDye = slot;
			SelectDyeIndex = index;
		}
		public static void CloseDyePicker() =>	SlotSelectDye = null;

		public static void OpenSetSelector() => DrawSetSelection = true;
		public static void CloseSetSelector() => DrawSetSelection = false;
		public static void OpenSetDyePicker() => DrawSetDyeSelection = true;
		public static void CloseSetDyePicker() => DrawSetDyeSelection = false;

		private static bool DrawVisibilityToggle(string id, bool hidden) {
			// this is fucking stupid
			var width = Math.Max(
				GuiHelpers.CalcIconSize(FontAwesomeIcon.EyeSlash).X,
				GuiHelpers.CalcIconSize(FontAwesomeIcon.Eye).X
			) + ImGui.GetStyle().ItemSpacing.X;

			return GuiHelpers.IconButtonTooltip(
				hidden ? FontAwesomeIcon.EyeSlash : FontAwesomeIcon.Eye,
				"Toggle visibility",
				new Vector2(width, 0),
				id
			);
		}

		public unsafe static void DrawControls() {
			if (GuiHelpers.IconButtonTooltip(FontAwesomeIcon.Tshirt, "Look up for a set."))
				OpenSetSelector();
			ImGui.SameLine();
			if (GuiHelpers.IconButtonTooltip(FontAwesomeIcon.PaintRoller, "Dye them all."))
				OpenSetDyePicker();

			if (DrawSetSelection)
				DrawSetSelectorList();
			if (DrawSetDyeSelection)
				DrawSetDyePicker();

			ImGui.Separator();
		}

		public unsafe static void DrawSelectorList(EquipSlot slot, object equipObj)
		{
			if (SlotItems == null)
				return;
			
			PopupSelect.HoverPopupWindow(
				PopupSelect.HoverPopupWindowFlags.SelectorList | PopupSelect.HoverPopupWindowFlags.SearchBar,
				SlotItems,
				(e, input) => e.Where(i => i.Name.Contains(input, StringComparison.OrdinalIgnoreCase)),
				(i, a) => (  // draw Line
						ImGui.Selectable(i.Name, a),
						ImGui.IsItemFocused()
				),
				(i) => { // on Select
					if (equipObj is ItemEquip item) {
						item.Id = i.Model.Id;
						item.Variant = (byte)i.Model.Variant;
						Target->Equip(SlotToIndex(slot), item);
					} else if (equipObj is WeaponEquip wep) {
						var isMain = slot == EquipSlot.MainHand;

						if (isMain) {
							wep.Set = i.Model.Id;
							wep.Base = i.Model.Base;
							wep.Variant = i.Model.Variant;
							Target->Equip(0, wep);
						}

						if (slot == EquipSlot.OffHand || i.SubModel.Id != 0) {
							var model = i.SubModel.Id != 0 ? i.SubModel : i.Model;
							wep.Set = model.Id;
							wep.Base = model.Base;
							wep.Variant = model.Variant;
							Target->Equip(1, wep);
						}
					}
				},
				CloseSelector,
				ref ItemSearch,
				"Item Select",
				"##equip_items",
				"##equip_search"
			);
		}

		private unsafe static void DrawGlassesSelector() {
			if (Glasses == null) return;
			
			PopupSelect.HoverPopupWindow(
				PopupSelect.HoverPopupWindowFlags.SelectorList | PopupSelect.HoverPopupWindowFlags.SearchBar,
				Glasses,
				(e, input) => e.Where(i => i.Name.Contains(input, StringComparison.OrdinalIgnoreCase)),
				(i, a) => (  // draw Line
						ImGui.Selectable(i.Name, a),
						ImGui.IsItemFocused()
					),
				(i) => { // on Select
					Target->SetGlasses((ushort)i.RowId);
				},
				() => DrawGlassesSelection = false,
				ref GlassesSearch,
				"Glasses Select",
				"##glasses_list",
				"##glasses_search"
			);
		}

		public unsafe static void DrawSetSelectorList()
		{
			IEnumerable<Set> sets = Sets.FindSets();

			if (!sets.Any())
				return;

			PopupSelect.HoverPopupWindow(
				PopupSelect.HoverPopupWindowFlags.SelectorList | PopupSelect.HoverPopupWindowFlags.SearchBar,
				sets,
				(e,input) => e.Where(i => i.Name.Contains(input, StringComparison.OrdinalIgnoreCase)),
				(i,a) => {
					bool selected = ImGui.Selectable(i.Name, a);
					bool focus = ImGui.IsItemFocused();

					ImGui.SameLine();
					ImGui.PushStyleVar(ImGuiStyleVar.Alpha, 0.5f);
					GuiHelpers.TextRight($"{i.Source}");
					ImGui.PopStyleVar();

					return (selected, focus);
				}, // draw Before Line
				(i) => Target->Equip(Sets.GetItems(i)), // on Select
				CloseSetSelector, // on close
				ref SetSearch,
				"Set Select",
				"##equip_sets",
				"##set_search"
			);
		}

		private static int DyeLastSubOrder = -1;
		private const int DyePickerWidth = 485;
		public static unsafe void DrawDyePicker(EquipSlot slot, object equipObj, int index)
		{
			PopupSelect.HoverPopupWindow(
				PopupSelect.HoverPopupWindowFlags.SearchBar
				| PopupSelect.HoverPopupWindowFlags.TwoDimenssion
				| PopupSelect.HoverPopupWindowFlags.Header,
				Dyes!,
				(e, input) => e.Where(i => i.Name.Contains(input, StringComparison.OrdinalIgnoreCase)),
				DrawDyePickerHeader,
				DrawDyePickerItem,
				(i) => { // on Select
					SetDye((IEquipItem)equipObj, slot, index, (byte)i.RowId);
				},
				CloseDyePicker, // on close
				ref DyeSearch,
				$"Dye {slot}",
				"",
				$"##dye_search",
				"Search...", // searchbar hint
				DyePickerWidth, // window width
				12 // number of columns
			);
		}
		public static unsafe void DrawSetDyePicker()
		{
			PopupSelect.HoverPopupWindow(
				PopupSelect.HoverPopupWindowFlags.SearchBar
				| PopupSelect.HoverPopupWindowFlags.TwoDimenssion
				| PopupSelect.HoverPopupWindowFlags.Header,
				Dyes!,
				(e, input) => e.Where(i => i.Name.Contains(input, StringComparison.OrdinalIgnoreCase)),
				DrawDyePickerHeader,
				DrawDyePickerItem,
				(i) => { // on Select
					foreach ((EquipSlot equipSlot, ItemCache itemCache) in Equipped)
					{
						var equip = itemCache.Equip;
						if (equip is WeaponEquip wep) {
							wep.Dye = (byte)i.RowId;
							Target->Equip((int)equipSlot, wep);
						} else if (equip is ItemEquip item) {
							item.Dye = (byte)i.RowId;
							Target->Equip(SlotToIndex(equipSlot), item);
						}
					}
				},
				CloseSetDyePicker, // on close
				ref DyeSearch,
				$"Dye All##dye_all",
				"",
				$"##dye_all_search##dye_all_search",
				"Search...", // searchbar hint
				DyePickerWidth, // window width
				12 // number of columns
			);
		}
		private static (bool, bool) DrawDyePickerItem(Dye i, bool isActive)
		{
			bool isThisRealNewLine = PopupSelect.HoverPopupWindowIndexKey % PopupSelect.HoverPopupWindowColumns == 0;
			bool isThisANewShade = i.SubOrder == 1;

			if (!isThisRealNewLine && isThisANewShade)
			{
				// skip some index key if we don't finish the row
				int howManyMissedButtons = 12 - (DyeLastSubOrder % 12);
				PopupSelect.HoverPopupWindowIndexKey += howManyMissedButtons;
			} else if (!isThisRealNewLine && !isThisANewShade)
				ImGui.SameLine();
			if (isThisANewShade)
				ImGui.Spacing();

			DyeLastSubOrder = i.SubOrder;

			// as we previously changed the index key, let's calculate calculate isActive again
			isActive = PopupSelect.HoverPopupWindowIndexKey == PopupSelect.HoverPopupWindowLastSelectedItemKey;

			if (isActive)
			{
				ImGui.PushStyleVar(ImGuiStyleVar.FrameBorderSize, 6f);
				ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 1f);
			}
			var selecting = ImGui.ColorButton($"{i.Name}##{i.RowId}", i.ColorVector4);
			if (isActive)
				ImGui.PopStyleVar(2);

			return (selecting, ImGui.IsItemFocused());
		}
		private static void DrawDyePickerHeader(object obj)
		{
			var i = (Dye)obj;
			// TODO: configuration to not show this
			var textSize = ImGui.CalcTextSize(i.Name);
			float dyeShowcaseWidth = (DyePickerWidth - textSize.X - (ImGui.GetStyle().ItemSpacing.X * 2)) / 2;
			ImGui.ColorButton($"{i.Name}##{i.RowId}##selected1", i.ColorVector4, ImGuiColorEditFlags.None, new Vector2(dyeShowcaseWidth, textSize.Y));
			ImGui.SameLine();
			ImGui.Text(i.Name);
			ImGui.SameLine();
			ImGui.ColorButton($"{i.Name}##{i.RowId}##selected2", i.ColorVector4, ImGuiColorEditFlags.None, new Vector2(dyeShowcaseWidth, textSize.Y));
		}
	}

	public class ItemCache : IDisposable {
		private CancellationTokenSource? _tokenSrc;
		
		private int? IconId;
		
		public object? Equip;
		public Item? Item;
		public ISharedImmediateTexture? Icon;

		public ItemCache(object? equip, EquipSlot slot)
			=> SetEquip(equip, slot);

		public void SetEquip(object? equip, EquipSlot slot) {
			Equip = equip;

			_tokenSrc?.Cancel();
			_tokenSrc = new CancellationTokenSource();
			Resolve(equip, slot, _tokenSrc.Token).ContinueWith(task => {
				if (task.Exception != null)
					Ktisis.Log.Error($"Error occurred while resolving item:\n{task.Exception}");
			}, TaskContinuationOptions.OnlyOnFaulted);
		}

		private async Task Resolve(object? equip, EquipSlot slot, CancellationToken token) {
			await Task.Yield();
			
			var item = equip != null ? EditEquip.FindItem(equip, slot) : null;
			if (token.IsCancellationRequested) return;
			Item = item;

			var newIconId = item?.Icon;
			if (newIconId != IconId) {
				var newIcon = newIconId is int id ? Services.Textures.GetFromGameIcon(id) : null;
				if (token.IsCancellationRequested)
					return;
				IconId = newIconId;
				Icon = newIcon;
			}
		}

		public void Dispose() {
			Icon = null;
		}

		~ItemCache() => Dispose();
	}
}
