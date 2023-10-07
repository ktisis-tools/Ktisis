using System;
using System.Collections.Generic;
using System.Numerics;

using ImGuiNET;

namespace Ktisis.Interface.Gui.Menus;

public interface IContextMenuNode {
	public void Draw();
}

public class ContextMenu {
	private readonly string Name;
	private readonly NodeContainer Nodes;
	
	private bool IsOpening = true;

	public ContextMenu(string id, List<IContextMenuNode> nodes) {
		this.Name = id;
		this.Nodes = new NodeContainer(nodes);
	}
	
	// Draw code

	public bool Draw() {
		if (this.IsOpening) {
			this.IsOpening = false;
			ImGui.OpenPopup(this.Name);
			return true;
		}
        
		if (!ImGui.BeginPopupContextWindow(this.Name))
			return false;
		
		this.Nodes.Draw();
		
		ImGui.EndPopup();
		return true;
	}
	
	// Nodes

	private class NodeContainer {
		private readonly List<IContextMenuNode> Nodes;

		public NodeContainer(List<IContextMenuNode> nodes) => this.Nodes = nodes;
		
		public void Draw() => this.Nodes.ForEach(node => node.Draw());
	}
	
	public class ActionItem : IContextMenuNode {
		private readonly string Name;
		private readonly Action Callback;

		private readonly string? Shortcut;

		public ActionItem(string name, Action cb) {
			this.Name = name;
			this.Callback = cb;
		}
		
		public ActionItem(string name, Action cb, string shortcut) {
			this.Name = name;
			this.Callback = cb;
			this.Shortcut = shortcut;
		}
		
		public void Draw() {
			var invoke = this.Shortcut switch {
				string => ImGui.MenuItem(this.Name, this.Shortcut),
				null => ImGui.MenuItem(this.Name)
			};
			
			if (invoke) this.Callback.Invoke();
		}
	}

	public class ActionGroup : IContextMenuNode {
		private readonly NodeContainer Nodes;

		public ActionGroup(List<IContextMenuNode> nodes) {
			this.Nodes = new NodeContainer(nodes);
		}

		public void Draw() => this.Nodes.Draw();
	}

	public class ActionSubMenu : IContextMenuNode {
		private readonly string Name;
		private readonly NodeContainer Nodes;

		public ActionSubMenu(string name, List<IContextMenuNode> nodes) {
			this.Name = name;
			this.Nodes = new NodeContainer(nodes);
		}
        
		public void Draw() {
			if (ImGui.BeginMenu(this.Name)) {
				this.Nodes.Draw();
				ImGui.EndMenu();
			}
		}
	}
	
	public class Separator : IContextMenuNode {
		public void Draw() => ImGui.Separator();
	}
}
