using System.Numerics;
using System.Collections.Generic;

using Dalamud.Interface;

using ImGuiNET;

using Ktisis.Services;
using Ktisis.Interface.Widgets;
using Ktisis.Scene.Interfaces;
using Ktisis.Interface;

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

		public List<Manipulable> GetDescendants() {
			var results = new List<Manipulable>();
			foreach (var child in Children) {
				results.Add(child);
				results.AddRange(child.GetDescendants());
			}
			return results;
		}

		public void DrawTreeNode() {
			if (!PreDraw()) return;

			if (this is IVisibilityToggle iVis) {
				var c = ImGui.GetCursorPosX();
				ImGui.SetCursorPosX(c + ImGui.GetContentRegionAvail().X - UiBuilder.IconFont.FontSize - ImGui.GetStyle().FramePadding.X);

				var rgba = ImGui.ColorConvertU32ToFloat4(Color);
				rgba.W = iVis.Visible ? 0.85f : 0.35f;

				ImGui.PushStyleColor(ImGuiCol.Text, ImGui.ColorConvertFloat4ToU32(rgba));
				ImGui.PushStyleColor(ImGuiCol.Button, 0);
				ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(0, 0));

				ImGui.BeginDisabled(!Ktisis.Configuration.ShowOverlay);
				if (Buttons.IconButton(FontAwesomeIcon.Eye, default, $"##Vis_{Name}_{KtisisGui.SequenceId++}")) {
					iVis.Visible = !iVis.Visible;

					if (!ImGui.IsKeyDown(ImGuiKey.LeftShift)) { // TODO: Config
						var desc = GetDescendants();
						foreach (var child in desc)
							if (child is IVisibilityToggle childVis)
								childVis.Visible = iVis.Visible;
					}
				}
				ImGui.EndDisabled();

				ImGui.PopStyleColor();
				ImGui.PopStyleColor();
				ImGui.PopStyleVar();

				ImGui.SameLine(c);
			}

			var flags = ImGuiTreeNodeFlags.None;
			if (EditorService.IsSelected(this))
				flags ^= ImGuiTreeNodeFlags.Selected;
			if (Children.Count == 0)
				flags ^= ImGuiTreeNodeFlags.Leaf ^ ImGuiTreeNodeFlags.Bullet;

			ImGui.PushStyleColor(ImGuiCol.Text, Color);
			ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(0, 0));
			var expand = Tree.CollapsibleNode(
				Name,
				flags ^ ImGuiTreeNodeFlags.NoTreePushOnOpen,
				UiSelect, Context
			);
			ImGui.PopStyleColor();
			ImGui.PopStyleVar();

			if (expand) {
				var indent = ImGui.GetStyle().FramePadding.X + ImGui.GetFontSize() / 2;

				if (Children.Count > 0) {
					//var start = Tree.LineStart();
					ImGui.Indent(indent);

					foreach (var child in Children)
						child.DrawTreeNode();

					ImGui.Unindent(indent);

					//Tree.LineEnd(start, Color);
				}
				//ImGui.TreePop();
			}
		}

		// Virtual methods

		public virtual bool PreDraw() => true;

		// Abstract methods

		public abstract string Name { get; set; }

		public virtual void Select()
			=> EditorService.Select(this);

		public virtual void Select(bool add)
			=> EditorService.Select(this, add);

		public virtual void UiSelect()
			=> Select(ImGui.IsKeyDown(ImGuiKey.LeftCtrl));

		public abstract void Context();
	}
}