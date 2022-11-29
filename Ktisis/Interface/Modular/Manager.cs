using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using ImGuiNET;

using Ktisis.Interface.Components;
using Ktisis.Util;

namespace Ktisis.Interface.Modular {
	internal class Manager {

		private const string NamespacePrefix = "Ktisis.Interface.Modular.ItemTypes.";

		private static readonly List<Type> AvailableContainers = Assembly.GetExecutingAssembly().GetTypes().Where(t => t.Namespace == NamespacePrefix + "Container").ToList();
		private static readonly List<Type> AvailableSpliters = Assembly.GetExecutingAssembly().GetTypes().Where(t => t.Namespace == NamespacePrefix + "Splitter").ToList();
		private static readonly List<Type> AvailablePanel = Assembly.GetExecutingAssembly().GetTypes().Where(t => t.Namespace == NamespacePrefix + "Panel").ToList();
		private static readonly List<Type> Available = AvailableContainers.Concat(AvailableSpliters).Concat(AvailablePanel).ToList();

		public static List<string> Handles = new();
		public static List<IModularItem> Config = new();

		public static void Init() {
			Handles.Clear();
			Config = ParseConfigList(Ktisis.Configuration.ModularConfig)!;
		}
		public static void Dispose() => Config = new();
		public static void Render() => Config?.ForEach(d => d.Draw());

		private static List<IModularItem>? ParseConfigList(List<ConfigObject>? configObjects) {
			if (configObjects == null) return null;

			List<IModularItem> configList = new();
			foreach (var o in configObjects) {
				var item = ParseConfigItem(o);
				if (item == null) continue;
				configList.Add(item);
			}
			if (configList.Any())
				return configList;
			return null;
		}
		private static IModularItem? ParseConfigItem(ConfigObject configObject) {

			// Get the type of desired instance
			Type? objectType = Available.FirstOrDefault(i => i.Name == configObject.Type);
			if (objectType == null) return null;

			// Get available constructors
			var constructors = objectType.GetConstructors(BindingFlags.Instance | BindingFlags.Public);
			if (constructors == null || constructors.Length == 0) return null;

			object? instance = null;

			// if parameterless constructor exists, use it
			if (constructors.Any(c => c.GetParameters().Length == 0)) {
				instance = Activator.CreateInstance(objectType);
				if (instance != null) return (IModularItem)instance;
			}

			// create possible parameters
			string handle = $"Window {Handles.Count}##Modular##{Handles.Count}";
			var items = ParseConfigList(configObject.Items)!;
			Dictionary<int, object[]?> paramSolutions = new() {
				{0, new object[] { Handles.Count, handle, items }},
				{1, new object[] { Handles.Count, handle,  }},
				{2, new object[] { items }},
			};
			Handles.Add(handle);

			// check if any constructor is compatible with out parameters
			var compatibleParamIndex = AnyCompatibleConstructors(paramSolutions, constructors);
			if (compatibleParamIndex != null)
				if (paramSolutions.TryGetValue((int)compatibleParamIndex, out object[]? parameters)) {
					instance = Activator.CreateInstance(objectType, parameters);
					if (instance != null) return (IModularItem)instance;
				}

			return null;
		}
		private static int? AnyCompatibleConstructors(Dictionary<int, object[]?> parametersSolutions, ConstructorInfo[]? constructors) {
			if (constructors == null) return null;
			foreach (var ctor in constructors) {
				int paramMatches = 0;
				foreach (var solu in parametersSolutions) {
					int i = 0;
					paramMatches = 0;
					foreach (var item in ctor.GetParameters()) {
						var paramSolution = solu.Value!.GetValue(i);
						i++;
						if (paramSolution == null) continue;

						if (paramSolution.GetType() == item.ParameterType)
							paramMatches++;
					}
					if (paramMatches == ctor.GetParameters().Length)
						return solu.Key;
				}
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
					bool selected = ImGui.Selectable($"{t.Name}##Modular##AddPanel##{t}", a);
					bool focus = ImGui.IsItemFocused();
					ImGui.SameLine();
					ImGui.PushStyleVar(ImGuiStyleVar.Alpha, 0.5f);
					GuiHelpers.TextRight(TypeToKind(t));
					ImGui.PopStyleVar();
					return (selected, focus);
				},
				(t) => Add(t.Name),
				()=> IsAddPanelOpen = false, // on close
				ref AddPanelSearch,
				"Add Panel",
				"##addpanel_select",
				"##addpanel_search");
		}
		private static string TypeToKind(Type? type) => type!.Namespace!.Split('.').Last();
		private static bool IsPanel(Type? type) => TypeToKind(type) == "Panel";
		private static bool IsContainer(Type? type) => TypeToKind(type) == "Container";
		private static bool IsAvailableContainer(string handle) => AvailableContainers.Any(a => a.Name == handle);
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
