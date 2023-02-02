using System;
using System.Numerics;

using ImGuiNET;

namespace Ktisis.Interface.Widgets {
	internal static class Tree {
		// Nodes

		internal static bool CollapsibleNode(string label, ImGuiTreeNodeFlags flags = 0, Action? leftClick = null, Action? rightClick = null) {
			var expand = ImGui.TreeNodeEx(label, ImGuiTreeNodeFlags.OpenOnArrow ^ flags);

			var rectMin = ImGui.GetItemRectMin() + new Vector2(ImGui.GetTreeNodeToLabelSpacing(), 0);
			var rectMax = ImGui.GetItemRectMax();

			var mousePos = ImGui.GetMousePos();

			var scrollMin = ImGui.GetScrollY();
			var scrollMax = ImGui.GetScrollMaxY();

			var canClick = mousePos.X > rectMin.X && mousePos.X < rectMax.X
				&& mousePos.Y > rectMin.Y && mousePos.Y < rectMax.Y;

			if (canClick) {
				if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
					leftClick?.Invoke();
				if (ImGui.IsMouseClicked(ImGuiMouseButton.Right))
					rightClick?.Invoke();
			}

			return expand;
		}

		internal static bool LeafNode(string label, Action? callback = null) {
			var res = CollapsibleNode(label, ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.Bullet, callback);
			if (res) ImGui.TreePop();
			return res;
		}

		// Lines

		internal static Vector2 LineStart() => new Vector2(
			ImGui.GetItemRectMin().X + (ImGui.GetTreeNodeToLabelSpacing() / 2),
			ImGui.GetItemRectMax().Y
		);

		internal static void LineEnd(Vector2 start, uint col, float thicc = 1) {
			var end = ImGui.GetItemRectMax();
			var draw = ImGui.GetWindowDrawList();
			draw.AddLine(
				start,
				new Vector2(start.X, end.Y),
				col,
				thicc
			);
		}
	}
}