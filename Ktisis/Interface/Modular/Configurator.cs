using System;
using System.Collections.Generic;
using System.Linq;

using ImGuiNET;

using Ktisis.Interface.Components;
using Ktisis.Util;

namespace Ktisis.Interface.Modular {
	internal class Configurator {

		// Below is config rendering logic
		private static bool IsAddPanelOpen = false;
		private static string AddPanelSearch = "";
		internal static ConfigObject? MovingObject = null;
		private static ConfigObject? SelectedObject = null;

		public static void DrawConfigTab(Configuration cfg) {
			if (ImGui.BeginTabItem("Modular")) {

				if (GuiHelpers.IconButton(Dalamud.Interface.FontAwesomeIcon.Plus))
					IsAddPanelOpen = true;

				if (ImGui.BeginChildFrame(958, new(ImGui.GetContentRegionAvail().X, ImGui.GetIO().DisplaySize.Y * 0.6f))) {
					var modularConfig = cfg.ModularConfig;
					modularConfig?.ForEach(c => TreeNode(c));
					ImGui.EndChildFrame();
				}

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
		private static bool IsPanel(Type? type) => TypeToKind(type) == "Panel";
		private static bool IsContainer(Type? type) => TypeToKind(type) == "Container";
		private static bool IsAvailableContainer(string handle) => Manager.AvailableContainers.Any(a => a.Name == handle);
		private static bool IsAvailablePanel(string? handle) => Manager.AvailablePanel.Any(a => a.Name == handle);
		private static bool IsAvailablePanel(ConfigObject? cfgObj) => IsAvailablePanel(cfgObj!.Type);
		private static void Add(string handle) => Add(new ConfigObject(handle));
		private static void Add(ConfigObject toAdd) {
			if (Ktisis.Configuration.ModularConfig == null)
				Ktisis.Configuration.ModularConfig = new();
			if (IsAvailableContainer(toAdd.Type))
				Ktisis.Configuration.ModularConfig.Add(toAdd);
			else
				if (Ktisis.Configuration.ModularConfig!.Any()) {

				if (Ktisis.Configuration.ModularConfig.Last()?.Items == null)
					Ktisis.Configuration.ModularConfig.Last().Items = new() { toAdd };
				else
					Ktisis.Configuration.ModularConfig.Last().Items?.Add(toAdd);
			}
			Manager.Init();
		}
		private static void Delete(ConfigObject toRemove) {
			DeleteSub(Ktisis.Configuration.ModularConfig, toRemove);
			Manager.Init();
		}
		private static void DeleteSub(List<ConfigObject> items, ConfigObject toRemove) {
			//int index = items.IndexOf(toRemove);
			//if(index != -1)
			//	for (int i = items.Count - 1; i >= 0; i--)
			//		if (i == index) items.RemoveAt(i);
			items.Remove(toRemove);
			items?.ForEach(cc => {
				if (cc.Items != null)
					DeleteSub(cc.Items, toRemove);
			});
		}
		private static void MoveAt(ConfigObject toMove, ConfigObject target) {
			if (target == null) return;

			if (!IsAvailablePanel(target) && (target.Items == null || !target.Items.Any())) {
				// if target is an empty container/splitter
				// drop it inside

				Delete(toMove);

				// add it in the items of target
				target.Items ??= new();
				target.Items.Add(toMove);

			} else {
				// if it's a panel or a filled container/splitter
				// drop it above

				Delete(toMove);
				InsertConfigBefore(toMove, target);
			}
			Manager.Init();
		}
		public static void InsertConfigBefore(ConfigObject itemtoInsert, ConfigObject itemBefore) {
			InsertBefore(Ktisis.Configuration.ModularConfig, itemtoInsert, itemBefore);
		}

		private static void InsertBefore(List<ConfigObject>? items, ConfigObject itemtoInsert, ConfigObject itemBefore) {
			if (items == null || !items.Any()) return;

			int index = items.FindIndex(r => r == itemBefore);
			if (index > -1)
				items.Insert(index, itemtoInsert);

			items.ForEach(co => InsertBefore(co.Items, itemtoInsert, itemBefore));
		}
		private static void MoveSource(ConfigObject source) =>
			MovingObject = source;

		private static void MoveTarget(ConfigObject target) {
			if (MovingObject == null) return;
			var movingTaget = MovingObject;
			MovingObject = null;
			MoveAt(movingTaget, target);
		}

		private unsafe static void TreeNode(ConfigObject cfgObj) {

			bool isLeaf = cfgObj.Items == null || !cfgObj.Items.Any();
			string handle = cfgObj.Type;
			string id = cfgObj.GetHashCode().ToString();

			bool open = ImGui.TreeNodeEx(id, ImGuiTreeNodeFlags.FramePadding | ImGuiTreeNodeFlags.DefaultOpen | (cfgObj == SelectedObject ? ImGuiTreeNodeFlags.Selected : 0) | (isLeaf ? ImGuiTreeNodeFlags.Leaf : ImGuiTreeNodeFlags.OpenOnArrow), handle);

			ImGui.PushID(id);
			if (ImGui.BeginPopupContextItem()) {
				if (GuiHelpers.IconButtonHoldConfirm(Dalamud.Interface.FontAwesomeIcon.Trash, $"Delete {handle}"))
					Delete(cfgObj);
				// Some processing...
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
					cfgObj.Items?.ForEach(c => TreeNode(c));

				ImGui.TreePop();
			}

		}
	}
}