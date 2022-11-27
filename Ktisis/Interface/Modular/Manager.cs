using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;

using Dalamud.Logging;
using ImGuiNET;

using Ktisis.Interface.Components;
using Ktisis.Util;

namespace Ktisis.Interface.Modular {
	internal class Manager {

		private static readonly List<Type> AvailableContainers = Assembly.GetExecutingAssembly().GetTypes()
					  .Where(t => t.Namespace == "Ktisis.Interface.Modular.ItemTypes.Container")
					  .ToList();
		private static readonly List<Type> Available = Assembly.GetExecutingAssembly().GetTypes()
					  .Where(t => t.Namespace == "Ktisis.Interface.Modular.ItemTypes")
					  .ToList();


		public static List<string> Handles = new();
		public static List<IModularItem> Config = new();

		public static void Init() {
			Handles.Clear();
			Config = ListConfigObjectToDelegate(Ktisis.Configuration.ModularConfig);
		}
		public static void Dispose() => Config = null;
		public static void Render() => Config?.ForEach(d => d.Draw());

		private static List<IModularItem>? ListConfigObjectToDelegate(List<ConfigObject>? configObjects) {
			if (configObjects == null) return null;

			List<IModularItem> listDelegateAndInfo = new();
			foreach (var o in configObjects) {
				var delegateAndInfo = ConfigObjectToDelegate(o);
				if (delegateAndInfo == null) continue;
				listDelegateAndInfo.Add(delegateAndInfo);
			}
			if (listDelegateAndInfo.Any())
				return listDelegateAndInfo;
			return null;
		}
		private static IModularItem? ConfigObjectToDelegate(ConfigObject configObject) {

			Type? type = Available.FirstOrDefault(i => i.Name == configObject.Type)?.DeclaringType;
			if (type == null) return null;


			string handle = $"Window {Handles.Count}##Modular##{Handles.Count}";
			var param = new object[] {
				Handles.Count,
				handle,
				ListConfigObjectToDelegate(configObject.Items)!
			};
			Handles.Add(handle);
			var modularObject = Activator.CreateInstance(type, param);

			MethodInfo? mi = type?.GetMethod(configObject.Type, BindingFlags.Public | BindingFlags.Static);
			if (modularObject != null) {

	

				if(configObject.Items != null)
					modularObject = ListConfigObjectToDelegate(configObject.Items);

				return (IModularItem)modularObject;
			}
			return null;
		}


		// Below is config rendering logic
		private static bool IsAddPanelOpen = false;
		private static string AddPanelSearch = "";

		public static void DrawConfigTab(Configuration cfg) {
			if (ImGui.BeginTabItem("Modular")) {

				if (ImGui.BeginChildFrame(958, new(ImGui.GetContentRegionAvail().X, 300))) {
					var modularConfig = cfg.ModularConfig;
					modularConfig?.ForEach(c => TreeNode(c));
					ImGui.EndChildFrame();
				}

				if (GuiHelpers.IconButton(Dalamud.Interface.FontAwesomeIcon.Plus))
					IsAddPanelOpen = true;

				ImGui.EndTabItem();
			}

			if (IsAddPanelOpen)
				DrawAddPanel();
		}

		private static void DrawAddPanel() {
			PopupSelect.HoverPopupWindow(
				PopupSelect.HoverPopupWindowFlags.SelectorList | PopupSelect.HoverPopupWindowFlags.SearchBar,
				Available,
				(e, input) => e.Where(t => t.Name.Contains(input, StringComparison.OrdinalIgnoreCase)),
				(t, a) => { // draw Line
					bool selected = ImGui.Selectable($"{t}##Modular##AddPanel##{t}", a);
					bool focus = ImGui.IsItemFocused();
					return (selected, focus);
				},
				(t) => Add(t.Name),
				()=> IsAddPanelOpen = false, // on close
				ref AddPanelSearch,
				"Add Panel",
				"##addpanel_select",
				"##addpanel_search");
		}
		private static bool IsContainer(string handle) => AvailableContainers.Any(a => a.Name == handle);
		private static void Add(string handle) => Add(new ConfigObject(handle));
		private static void Add(ConfigObject toAdd) {
			if (Ktisis.Configuration.ModularConfig == null)
				Ktisis.Configuration.ModularConfig = new();
			if (IsContainer(toAdd.Type))
				Ktisis.Configuration.ModularConfig.Add(toAdd);
			else
				if (Ktisis.Configuration.ModularConfig!.Any()) {

				if (Ktisis.Configuration.ModularConfig.Last()?.Items == null)
					Ktisis.Configuration.ModularConfig.Last().Items = new() { toAdd };
				else
					Ktisis.Configuration.ModularConfig.Last().Items?.Add(toAdd);
			}
			Init();
		}
		private static void Delete(ConfigObject toRemove) {
			Ktisis.Configuration.ModularConfig?.ForEach(c => DeleteSub(c, toRemove));
			Ktisis.Configuration.ModularConfig?.Remove(toRemove);
			Init();
		}
		private static void DeleteSub(ConfigObject parent,ConfigObject toRemove) {
			parent.Items?.Remove(toRemove);
			parent.Items?.ForEach(cc => DeleteSub(cc, toRemove));
		}


		private static void TreeNode(ConfigObject cfgObj, bool selected = false) {

			bool isLeaf = cfgObj.Items == null || !cfgObj.Items.Any();
			string handle = cfgObj.Type;
			string id = cfgObj.GetHashCode().ToString();

			bool open = ImGui.TreeNodeEx(id, ImGuiTreeNodeFlags.FramePadding | ImGuiTreeNodeFlags.DefaultOpen | (selected ? ImGuiTreeNodeFlags.Selected : 0) | (isLeaf ? ImGuiTreeNodeFlags.Leaf : ImGuiTreeNodeFlags.OpenOnArrow), handle);

			ImGui.PushID(id);
			if (ImGui.BeginPopupContextItem()) {
				if (GuiHelpers.IconButtonHoldConfirm(Dalamud.Interface.FontAwesomeIcon.Trash, $"Delete {handle}"))
					Delete(cfgObj);
				// Some processing...
				ImGui.EndPopup();
			}
			ImGui.PopID();

			if (ImGui.IsItemClicked()) {
				// Some processing...
			}
			if (ImGui.IsItemClicked() && ImGui.GetIO().KeyCtrl && ImGui.GetIO().KeyShift)
				Delete(cfgObj);

			if (ImGui.BeginDragDropTarget()) {
				// Some processing...
				ImGui.EndDragDropTarget();
			}

			if (ImGui.BeginDragDropSource()) {
				// Some processing...
				ImGui.EndDragDropSource();
			}

			if (open) {
				// Recursive call...
				if(!isLeaf)
					cfgObj.Items?.ForEach(c => TreeNode(c));

				ImGui.TreePop();
			}
		}
	}

	public class ContentsInfo {
		public string Handle = "##modular##handle";
		public List<(Delegate, ContentsInfo)>? Actions = null;
	}

	[Serializable]
	public class ConfigObject {
		public string Type;
		public List<ConfigObject>? Items;

		public ConfigObject(string type, List<ConfigObject>? items = null) {
			this.Type = type;
			this.Items = items;
		}
	}
}
