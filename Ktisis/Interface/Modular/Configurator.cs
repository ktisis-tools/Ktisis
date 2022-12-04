using System;
using System.Collections.Generic;
using System.Linq;

using Dalamud.Interface;
using ImGuiNET;

using Ktisis.Interface.Components;
using Ktisis.Util;

namespace Ktisis.Interface.Modular {
	internal class Configurator {

		// Below is config rendering logic
		private enum AddItemListMode { Closed, Last, AboveSelected };
		private static AddItemListMode IsAddPanelOpen = AddItemListMode.Closed;
		private static string AddPanelSearch = "";
		internal static IModularItem? MovingItem = null;
		private static IModularItem? SelectedItem = null;

		public static void DrawConfigTab(Configuration cfg) {
			if (ImGui.BeginTabItem("Modular")) {

				if (GuiHelpers.IconButton(FontAwesomeIcon.Plus))
					IsAddPanelOpen = AddItemListMode.Last;
				ImGui.SameLine();
				if (GuiHelpers.IconButton(FontAwesomeIcon.Clipboard, default, $"Export##Modular"))
					Misc.ExportClipboard(Ktisis.Configuration.ModularConfig);
				ImGui.SameLine();
				if (GuiHelpers.IconButtonHoldConfirm(FontAwesomeIcon.Paste, $"Hold Ctrl and Shift to paste and replace the entire modular configuration.", default, $"Import##Modular")) {
					var importModular = Misc.ImportClipboard<List<IModularItem>>();
					if (importModular != null)
						Ktisis.Configuration.ModularConfig = importModular;
				}

				ImGui.SameLine();
				var hideDefaultWindow = Ktisis.Configuration.ModularHideDefaultWorkspace;
				if (ImGui.Checkbox("Hide Default Window", ref hideDefaultWindow))
					Ktisis.Configuration.ModularHideDefaultWorkspace = hideDefaultWindow;

				ImGui.Columns(2);
				if (ImGui.BeginChildFrame(958, new(ImGui.GetContentRegionAvail().X, ImGui.GetIO().DisplaySize.Y * 0.6f))) {
					foreach (var item in cfg.ModularConfig) {
						if (TreeNode(item))
							break;
					}
					ImGui.EndChildFrame();
				}

				ImGui.NextColumn();
				DrawItemDetails(SelectedItem);
				ImGui.Columns();

				ImGui.EndTabItem();
			}

			if (IsAddPanelOpen != AddItemListMode.Closed)
				DrawAddPanel();
		}

		private static void DrawAddPanel() {
			PopupSelect.HoverPopupWindow(
				PopupSelect.HoverPopupWindowFlags.SelectorList | PopupSelect.HoverPopupWindowFlags.SearchBar,
				Manager.Available,
				(e, input) => e.Where(t => t.Name.Contains(input, StringComparison.OrdinalIgnoreCase)),
				(t, a) => { // draw Line
					bool selected = ImGui.Selectable($"{t.Name}##Modular##AddPanel##{t}", a);
					bool focus = ImGui.IsItemFocused();
					ImGui.SameLine();
					ImGui.PushStyleVar(ImGuiStyleVar.Alpha, 0.5f);
					GuiHelpers.TextRight(TypeToKind(t));
					ImGui.PopStyleVar();
					return (selected, focus);
				},
				(t) => {
					if (SelectedItem != null && IsAddPanelOpen == AddItemListMode.AboveSelected)
						AddBefore(t.Name, SelectedItem);
					else if (IsAddPanelOpen == AddItemListMode.Last)
						Add(t.Name);
				},
				() => IsAddPanelOpen = AddItemListMode.Closed, // on close
				ref AddPanelSearch,
				"Add Panel",
				"##addpanel_select",
				"##addpanel_search");
		}
		private static string TypeToKind(Type? type) => type!.Namespace!.Split('.').Last();
		private static void Add(string typeName) {
			var item = Manager.CreateItemFromTypeName(typeName);
			if (item == null) return;
			Add(item);
		}


		private static void Add(IModularItem toAdd) {
			if (Ktisis.Configuration.ModularConfig == null)
				Ktisis.Configuration.ModularConfig = new();

			if (toAdd is IModularContainer)
				Ktisis.Configuration.ModularConfig.Add(toAdd);
			else if (Ktisis.Configuration.ModularConfig.Any()) {
				var container = (IModularContainer)Ktisis.Configuration.ModularConfig.Last();
				container.Items.Add(toAdd);
			}
			Manager.Init();
		}
		private static void AddBefore(string handle, IModularItem itemBefore) {
			var item = Manager.CreateItemFromTypeName(handle);
			if (item == null) return;
			InsertBefore(Ktisis.Configuration.ModularConfig, item, itemBefore);
		}

		private static bool Delete(IModularItem toRemove) {
			var pair = DeleteSub(Ktisis.Configuration.ModularConfig, toRemove);
			if (pair != null) {
				pair.Value.Item1.RemoveAt(pair.Value.Item2);
				Manager.Init();
				return true;
			}
			return false;
		}
		private static (List<IModularItem>,int)? DeleteSub(List<IModularItem> items, IModularItem toRemove) {
			var index = items.IndexOf(toRemove);
			if (index != -1) return (items, index);

			foreach (var cc in items) {
				if (cc is IModularContainer container && container.Items.Any()) {
					var pair = DeleteSub(container.Items, toRemove);
					if (pair != null)
						return pair;
				}
			}
			return null;
		}
		private static bool MoveAt(IModularItem toMove, IModularItem target) {
			if (target == null) return false;
			var isDeleted = false;

			if (target is IModularContainer container && !container.Items.Any()) {
				// if target is an empty container/splitter
				// drop it inside

				isDeleted |= Delete(toMove);

				// add it in the items of target
				((IModularContainer)target).Items.Add(toMove);

			} else {
				// if it's a panel or a filled container/splitter
				// drop it above

				isDeleted |= Delete(toMove);
				InsertConfigBefore(toMove, target);
			}
			Manager.Init();
			return isDeleted;
		}
		private static void InsertConfigBefore(IModularItem itemtoInsert, IModularItem itemBefore) {
			InsertBefore(Ktisis.Configuration.ModularConfig, itemtoInsert, itemBefore);
		}

		private static void InsertBefore(List<IModularItem>? items, IModularItem itemtoInsert, IModularItem itemBefore) {
			if (items == null || !items.Any()) return;

			int index = items.FindIndex(r => r == itemBefore);
			if (index > -1)
				items.Insert(index, itemtoInsert);

			items.ForEach(co => InsertBefore(co is IModularContainer container ? container.Items : null, itemtoInsert, itemBefore));
		}
		private static void MoveSource(IModularItem source) =>
			MovingItem = source;

		private static bool MoveTarget(IModularItem target) {
			if (MovingItem == null) return false;
			var movingTaget = MovingItem;
			MovingItem = null;
			return MoveAt(movingTaget, target);
		}

		private unsafe static bool TreeNode(IModularItem item) {
			bool isLeaf = !(item is IModularContainer container && container.Items.Any());

			string handle = item.GetType().Name;
			string id = item.GetHashCode().ToString();

			bool open = ImGui.TreeNodeEx(id, ImGuiTreeNodeFlags.FramePadding | ImGuiTreeNodeFlags.DefaultOpen | (item == SelectedItem ? ImGuiTreeNodeFlags.Selected : 0) | (isLeaf ? ImGuiTreeNodeFlags.Leaf : ImGuiTreeNodeFlags.OpenOnArrow), handle);
			bool iteratorModified = false;
			ImGui.PushID(id);
			if (ImGui.BeginPopupContextItem()) {
				iteratorModified |= DrawContextMenu(item);
				ImGui.EndPopup();
			}
			ImGui.PopID();

			if (ImGui.IsItemClicked()) {
				SelectedItem = item;
			}
			if (ImGui.IsItemClicked() && ImGui.GetIO().KeyCtrl && ImGui.GetIO().KeyShift)
				iteratorModified |= Delete(item);

			if (ImGui.BeginDragDropTarget()) {

				// highlight target
				ImGui.AcceptDragDropPayload("_IModularItem");

				// Small hack to fire MoveTarget() on mouse button release
				if (MovingItem != null && !ImGui.GetIO().MouseDown[(int)ImGuiMouseButton.Left])
					iteratorModified |= MoveTarget(item);

				ImGui.EndDragDropTarget();
			}

			if (ImGui.BeginDragDropSource()) {
				ImGui.SetDragDropPayload("_IModularItem", IntPtr.Zero, 0);

				// Small hack to set the move source on mouse button hold
				if (ImGui.GetIO().MouseDownDuration[(int)ImGuiMouseButton.Left] < 0.5f)
					MoveSource(item);

				ImGui.EndDragDropSource();
			}


			if (open) {
				// Recursive call...
				if (!isLeaf && !iteratorModified)
					foreach (var child in ((IModularContainer)item).Items) {
						iteratorModified |= TreeNode(child);
						if (iteratorModified)
							break;
					}

				ImGui.TreePop();
			}
			return iteratorModified;
		}
		private static bool DrawContextMenu(IModularItem item) {
			SelectedItem = item;
			if (GuiHelpers.IconButtonTooltip(FontAwesomeIcon.Plus, $"Add above {item.GetType().Name}"))
				IsAddPanelOpen = AddItemListMode.AboveSelected;

			ImGui.SameLine();
			if (GuiHelpers.IconButtonHoldConfirm(FontAwesomeIcon.Trash, $"Delete {item.GetType().Name}"))
				if (Delete(item))
					return true;

			return false;
		}
		private static void DrawItemDetails(IModularItem? item) {
			if (item == null) return;
			item.DrawConfig();
		}

		internal static bool DrawBitwiseFlagSelect<TEnum>(string label, ref TEnum flagStack) where TEnum : struct, Enum =>
			DrawBitwiseFlagSelect<TEnum>(label, ref flagStack, Enum.GetValues<TEnum>().ToList());
		internal static bool DrawBitwiseFlagSelect<TEnum>(string label, ref TEnum flagStack, List<TEnum> whitelist) where TEnum : struct, Enum =>
			DrawFlagSelect<TEnum>(label, ref flagStack, whitelist, true);
		internal static bool DrawFlagSelect<TEnum>(string label, ref TEnum flagStack) where TEnum : struct, Enum =>
			DrawFlagSelect<TEnum>(label, ref flagStack, Enum.GetValues<TEnum>().ToList(), false);
		internal static bool DrawFlagSelect<TEnum>(string label, ref TEnum flagStack, List<TEnum> whitelist) where TEnum : struct, Enum =>
			DrawFlagSelect<TEnum>(label, ref flagStack, whitelist, false);
		internal static bool DrawFlagSelect<TEnum>(string label, ref TEnum flagStack, List<TEnum> whitelist, bool bitwise) where TEnum : struct, Enum {
			if (!ImGui.CollapsingHeader(label)) return false;

			ImGui.TextWrapped(flagStack.ToString());

			bool active = false;
			int intFlagStack = Convert.ToInt32(flagStack);
			active |= ImGui.InputInt($"##{label}##DrawFlagSelect##IntInput", ref intFlagStack, 1, 10, ImGuiInputTextFlags.EnterReturnsTrue);

			ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
			if (ImGui.BeginListBox($"##{label}##DrawFlagSelect")) {

				foreach (var flag in whitelist.Cast<int>()) {

					bool hasFlag = bitwise? (intFlagStack & flag) != 0 : intFlagStack == flag;
					if (ImGui.Selectable($"{(TEnum)Enum.ToObject(typeof(TEnum), flag)}##Modular##Details##{typeof(TEnum).Name}", hasFlag)) {
						active |= true;
						if (bitwise) {
							if (!hasFlag)
								intFlagStack |= flag;
							else
								intFlagStack &= ~flag;
						} else {
							intFlagStack = flag;
						}
					}
				}
			}
			ImGui.EndListBox();
			if (active)
				flagStack = (TEnum)Enum.ToObject(typeof(TEnum), intFlagStack);
			return active;
		}
	}
}