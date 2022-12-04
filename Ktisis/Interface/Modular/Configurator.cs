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
		private static bool IsAddPanelOpen = false;
		private static string AddPanelSearch = "";
		internal static IModularItem? MovingObject = null;
		private static IModularItem? SelectedObject = null;

		public static void DrawConfigTab(Configuration cfg) {
			if (ImGui.BeginTabItem("Modular")) {

				if (GuiHelpers.IconButton(FontAwesomeIcon.Plus))
					IsAddPanelOpen = true;
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
					cfg.ModularConfig.ForEach(c => TreeNode(c));
					ImGui.EndChildFrame();
				}

				ImGui.NextColumn();
				DrawItemDetails(SelectedObject);
				ImGui.Columns();

				ImGui.EndTabItem();
			}

			if (IsAddPanelOpen)
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
				(t) => Add(t.Name),
				() => IsAddPanelOpen = false, // on close
				ref AddPanelSearch,
				"Add Panel",
				"##addpanel_select",
				"##addpanel_search");
		}
		private static string TypeToKind(Type? type) => type!.Namespace!.Split('.').Last();
		private static void Add(string typeName) {
			var obj = Manager.CreateItemFromTypeName(typeName);
			if (obj == null) return;
			Add(obj);
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
		public static void AddBefore(string handle, IModularItem itemBefore) {
			var obj = Manager.CreateItemFromTypeName(handle);
			if (obj == null) return;
			InsertBefore(Ktisis.Configuration.ModularConfig, obj, itemBefore);
		}

		private static void Delete(IModularItem toRemove) {
			var pair = DeleteSub(Ktisis.Configuration.ModularConfig, toRemove);
			if (pair != null) {
				pair.Value.Item1.RemoveAt(pair.Value.Item2);
				Manager.Init();
			}
		}
		private static (List<IModularItem>,int)? DeleteSub(List<IModularItem> items, IModularItem toRemove) {
			items.Remove(toRemove);
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
		private static void MoveAt(IModularItem toMove, IModularItem target) {
			if (target == null) return;

			if (target is IModularContainer container && !container.Items.Any()) {
				// if target is an empty container/splitter
				// drop it inside

				Delete(toMove);

				// add it in the items of target
				((IModularContainer)target).Items.Add(toMove);

			} else {
				// if it's a panel or a filled container/splitter
				// drop it above

				Delete(toMove);
				InsertConfigBefore(toMove, target);
			}
			Manager.Init();
		}
		public static void InsertConfigBefore(IModularItem itemtoInsert, IModularItem itemBefore) {
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
			MovingObject = source;

		private static void MoveTarget(IModularItem target) {
			if (MovingObject == null) return;
			var movingTaget = MovingObject;
			MovingObject = null;
			MoveAt(movingTaget, target);
		}

		private unsafe static void TreeNode(IModularItem cfgObj) {
			bool isLeaf = !(cfgObj is IModularContainer container && container.Items.Any());

			string handle = cfgObj.GetType().Name;
			string id = cfgObj.GetHashCode().ToString();

			bool open = ImGui.TreeNodeEx(id, ImGuiTreeNodeFlags.FramePadding | ImGuiTreeNodeFlags.DefaultOpen | (cfgObj == SelectedObject ? ImGuiTreeNodeFlags.Selected : 0) | (isLeaf ? ImGuiTreeNodeFlags.Leaf : ImGuiTreeNodeFlags.OpenOnArrow), handle);

			ImGui.PushID(id);
			if (ImGui.BeginPopupContextItem()) {
				DrawContextMenu(cfgObj);
				ImGui.EndPopup();
			}
			ImGui.PopID();

			if (ImGui.IsItemClicked()) {
				SelectedObject = cfgObj;
			}
			if (ImGui.IsItemClicked() && ImGui.GetIO().KeyCtrl && ImGui.GetIO().KeyShift)
				Delete(cfgObj);

			if (ImGui.BeginDragDropTarget()) {

				// highlight target
				ImGui.AcceptDragDropPayload("_ConfigObject");

				// Small hack to fire MoveTarget() on mouse button release
				if (MovingObject != null && !ImGui.GetIO().MouseDown[(int)ImGuiMouseButton.Left])
					MoveTarget(cfgObj);

				ImGui.EndDragDropTarget();
			}

			if (ImGui.BeginDragDropSource()) {
				ImGui.SetDragDropPayload("_ConfigObject", IntPtr.Zero, 0);

				// Small hack to set the move source on mouse button hold
				if (ImGui.GetIO().MouseDownDuration[(int)ImGuiMouseButton.Left] < 0.5f)
					MoveSource(cfgObj);

				ImGui.EndDragDropSource();
			}

			if (open) {
				// Recursive call...
				if (!isLeaf)
					((IModularContainer)cfgObj).Items.ForEach(c => TreeNode(c));

				ImGui.TreePop();
			}

		}
		private static void DrawContextMenu(IModularItem cfgObj) {
			if (GuiHelpers.IconButtonHoldConfirm(FontAwesomeIcon.Trash, $"Delete {cfgObj.GetType()}"))
				Delete(cfgObj);

			//ImGui.SameLine();
			//if (GuiHelpers.IconButtonHoldConfirm(FontAwesomeIcon.Plus, $"Add above {cfgObj.Type}"))
			// TODO open DrawAddPanel and execute AddBefore() on select
		}
		private static void DrawItemDetails(IModularItem? cfgObj) {
			if (cfgObj == null) return;
			cfgObj.DrawConfig();
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