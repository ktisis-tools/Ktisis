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

		private static MethodInfo[] AvailableContainers = typeof(Container).GetMethods(BindingFlags.Public | BindingFlags.Static);
		private static MethodInfo[] AvailableSpliters = typeof(Spliter).GetMethods(BindingFlags.Public | BindingFlags.Static);
		private static MethodInfo[] AvailablePanels = typeof(Panel).GetMethods(BindingFlags.Public | BindingFlags.Static);
		private static List<string> Available = AvailableContainers.Select(a => a.Name).Concat(AvailableSpliters.Select(a => a.Name)).Concat(AvailablePanels.Select(a => a.Name)).ToList();


		public delegate void CI(ContentsInfo ci);
		public static List<(Delegate, ContentsInfo)>? Config = new();

		public static List<string> Handles = new();

		public static void Init() {
			Handles.Clear();
			Config = ListConfigObjectToDelegate(Ktisis.Configuration.ModularConfig);
		}
		public static void Dispose() => Config = null;
		public static void Render() => Config?.ForEach(d => d.Item1.DynamicInvoke(d.Item2));



		public static List<(Delegate, ContentsInfo)>? ListConfigObjectToDelegate(List<ConfigObject>? configObjects) {
			if (configObjects == null) return null;
			List<(Delegate, ContentsInfo)> listDelegateAndInfo = new();
			foreach (var o in configObjects) {
				var delegateAndInfo = ConfigObjectToDelegate(o);
				if (delegateAndInfo == null) continue;
				listDelegateAndInfo.Add(((Delegate, ContentsInfo))delegateAndInfo);
			}
			if (listDelegateAndInfo.Any())
				return listDelegateAndInfo;
			return null;
		}
		public static (Delegate, ContentsInfo)? ConfigObjectToDelegate(ConfigObject configObject) {

			Type? type = AvailableContainers.FirstOrDefault(i => i.Name == configObject.Name)?.DeclaringType;
			if (type == null)
				type = AvailableSpliters.FirstOrDefault(i => i.Name == configObject.Name)?.DeclaringType;
			if (type == null)
				type = AvailablePanels.FirstOrDefault(i => i.Name == configObject.Name)?.DeclaringType;
			if (type == null) return null;

			MethodInfo? mi = type?.GetMethod(configObject.Name, BindingFlags.Public | BindingFlags.Static);
			if (mi != null) {

				var reflectionDelgate = Delegate.CreateDelegate(typeof(CI), mi);

				string handle = $"Window {Handles.Count}##Modular##{Handles.Count}";
				Handles.Add(handle);

				List<(Delegate, ContentsInfo)>? actions = null;
				if(configObject.Contents != null)
					actions = ListConfigObjectToDelegate(configObject.Contents);

				var ci = new ContentsInfo {
					Handle = handle,
					Actions = actions
				};

				return (reflectionDelgate, ci);
			}
			return null;
		}


		// Below is config rendering logic
		private static bool IsAddPanelOpen = false;
		private static string AddPanelSearch = "";

		public static void DrawConfigTab(Configuration cfg) {
			if (ImGui.BeginTabItem("Modular")) {

				if(ImGui.BeginChildFrame(958,new(ImGui.GetContentRegionAvail().X,300))){
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
				(e, input) => e.Where(t => t.Contains(input, StringComparison.OrdinalIgnoreCase)),
				(t, a) => { // draw Line
					bool selected = ImGui.Selectable($"{t}##Modular##AddPanel##{t}", a);
					bool focus = ImGui.IsItemFocused();
					return (selected, focus);
				},
				(t) => { // on Select
					PluginLog.Debug($"adding {t}");
					ConfigObject addConfigObject = new ConfigObject(t);
					if (Ktisis.Configuration.ModularConfig == null)
						Ktisis.Configuration.ModularConfig = new();
					if (IsContainer(t))
						Ktisis.Configuration.ModularConfig.Add(addConfigObject);
					else
						if (Ktisis.Configuration.ModularConfig!.Any()) {

						if (Ktisis.Configuration.ModularConfig.Last()?.Contents == null)
							Ktisis.Configuration.ModularConfig.Last().Contents = new() { addConfigObject };
						else
							Ktisis.Configuration.ModularConfig.Last().Contents?.Add(addConfigObject);
					}
					Init();
				},
				()=> IsAddPanelOpen = false, // on close
				ref AddPanelSearch,
				"Add Panel",
				"##addpanel_select",
				"##addpanel_search");
		}

		public static bool IsContainer(string handle) => AvailableContainers.Any(a => a.Name == handle);
		public static void DeleteCfgObject(ConfigObject toRemove) {
			Ktisis.Configuration.ModularConfig?.ForEach(c => DeleteSubCfgObject(c, toRemove));
			Ktisis.Configuration.ModularConfig?.Remove(toRemove);
			Init();
		}
		public static void DeleteSubCfgObject(ConfigObject parent,ConfigObject toRemove) {
			parent.Contents?.Remove(toRemove);
			parent.Contents?.ForEach(cc => DeleteSubCfgObject(cc, toRemove));
		}


		private static void TreeNode(ConfigObject cfgObj, bool selected = false) {

			bool isLeaf = cfgObj.Contents == null || !cfgObj.Contents.Any();
			string handle = cfgObj.Name;
			string id = cfgObj.GetHashCode().ToString();
			//PluginLog.Debug($"modular node {handle} {isLeaf} {cfgObj.Contents?.Count}");

			bool open = ImGui.TreeNodeEx(id, ImGuiTreeNodeFlags.FramePadding | ImGuiTreeNodeFlags.DefaultOpen | (selected ? ImGuiTreeNodeFlags.Selected : 0) | (isLeaf ? ImGuiTreeNodeFlags.Leaf : ImGuiTreeNodeFlags.OpenOnArrow), handle);

			ImGui.PushID(id);
			if (ImGui.BeginPopupContextItem()) {
				if (GuiHelpers.IconButtonHoldConfirm(Dalamud.Interface.FontAwesomeIcon.Trash, $"Delete {handle}"))
					DeleteCfgObject(cfgObj);
				// Some processing...
				ImGui.EndPopup();
			}
			ImGui.PopID();

			if (ImGui.IsItemClicked()) {
				// Some processing...
			}
			if (ImGui.IsItemClicked() && ImGui.GetIO().KeyCtrl && ImGui.GetIO().KeyShift)
				DeleteCfgObject(cfgObj);

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
					cfgObj.Contents?.ForEach(c => TreeNode(c));

				ImGui.TreePop();
			}
		}
	}

	internal class ModItem {
		public StringHandle handle { get; set; }
	}
	public class ContentsInfo {
		public string Handle = "##modular##handle";
		public List<(Delegate, ContentsInfo)>? Actions = null;
	}

	[Serializable]
	public class ConfigObject {
		public string Name;
		public List<ConfigObject>? Contents;

		public ConfigObject(string name, List<ConfigObject>? contents = null) {
			this.Name = name;
			this.Contents = contents;
		}
	}
}
