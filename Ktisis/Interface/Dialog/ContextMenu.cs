using System;
using System.Numerics;
using System.Collections.Generic;

using ImGuiNET;

namespace Ktisis.Interface.Dialog {
	public class ContextMenu : KtisisWindow {
		// ContextMenuItem

		public class ContextMenuItem {
			public string Label = "";
			public Action? Callback = null;
			public bool Separator = false;

			public ContextMenuItem(string label, Action? callback) {
				Label = label;
				Callback = callback;
			}
		}

		// ContextMenu

		private const string Name = "##Ktisis_ContextMenu";

		public ContextMenu() : base(
			Name,
			ImGuiWindowFlags.NoDecoration
			^ ImGuiWindowFlags.NoMove
			^ ImGuiWindowFlags.AlwaysAutoResize
		) {
			Items = new();
		}

		public List<ContextMenuItem> Items;

		public void AddItem(string label, Action? callback) => Items.Add(new ContextMenuItem(label, callback));
		public void AddSection(Dictionary<string, Action> items, bool separator = true) {
			var list = new List<ContextMenuItem>();
			foreach (var (label, callback) in items)
				list.Add(new ContextMenuItem(label, callback));
			AddSection(list, separator);
		}

		private void AddItems(List<ContextMenuItem> items) => Items.AddRange(items);
		private void AddSection(List<ContextMenuItem> items, bool separator = true) {
			if (separator && Items.Count > 0 && items.Count > 0)
				items[0].Separator = true;
			AddItems(items);
		}

		public override void Draw() {
			foreach (var item in Items) {
				if (item.Separator)
					ImGui.Separator();
				if (ImGui.Selectable(item.Label))
					item.Callback?.Invoke();
			}

			if (!ImGui.IsWindowFocused())
				Close();
		}

		public override void OnOpen() {
			base.OnOpen();
			Position = ImGui.GetMousePos() + new Vector2(20, 0);
			ImGui.SetNextWindowPos((Vector2)Position);
			ImGui.SetNextWindowFocus();
		}

		public override void OnClose() {
			Items.Clear();
			base.OnClose();
		}
	}
}