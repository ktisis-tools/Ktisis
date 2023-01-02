using System.Collections.Generic;

using ImGuiNET;

using Ktisis.Services;
using Ktisis.Interface.Widgets;

namespace Ktisis.Scene {
	public abstract class Manipulable {
		// Properties

		public virtual uint Color => 0xFFFFFFFF;

		public Manipulable? Parent;
		public List<Manipulable> Children = new();

		// Base methods

		public bool IsSelected() => EditorService.IsSelected(this);

		public Manipulable AddChild(Manipulable item) {
			if (item.Parent != null)
				item.Parent.RemoveChild(item);
			item.Parent = this;
			Children.Add(item);
			return item;
		}

		public void RemoveChild(Manipulable item) {
			item.Parent = null;
			Children.Remove(item);
		}

		public void DrawTreeNode() {
			if (!PreDraw()) return;

			var flags = ImGuiTreeNodeFlags.None;
			if (EditorService.IsSelected(this))
				flags ^= ImGuiTreeNodeFlags.Selected;
			if (Children.Count == 0)
				flags ^= ImGuiTreeNodeFlags.Leaf ^ ImGuiTreeNodeFlags.Bullet;

			ImGui.PushStyleColor(ImGuiCol.Text, Color);
			var expand = Tree.CollapsibleNode(
				Name,
				flags,
				Select, Context
			);
			ImGui.PopStyleColor();

			if (expand) {
				if (Children.Count > 0) {
					var start = Tree.LineStart();

					foreach (var child in Children)
						child.DrawTreeNode();

					Tree.LineEnd(start, Color);
				}
				ImGui.TreePop();
			}
		}

		// Virtual methods

		public virtual bool PreDraw() => true;

		// Abstract methods

		public abstract string Name { get; set; }

		public abstract void Select();
		public abstract void Context();
	}

	public interface Transformable {
		// TODO: Unified class for transform types?
		public abstract object? GetTransform();
		public abstract void SetTransform(object trans);
	}
}