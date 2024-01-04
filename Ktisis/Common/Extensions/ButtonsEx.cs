using System.Numerics;

using ImGuiNET;

namespace Ktisis.Common.Extensions;

public static class ButtonsEx {
	internal static bool IsClicked() {
		var min = ImGui.GetItemRectMin();
		var max = ImGui.GetItemRectMax();
		return ImGui.IsMouseHoveringRect(min, max) && ImGui.IsMouseClicked(ImGuiMouseButton.Left);
	}
	
	internal static bool IsClicked(Vector2 margin) {
		var min = ImGui.GetItemRectMin() - margin;
		var max = ImGui.GetItemRectMax() + margin;
		return ImGui.IsMouseHoveringRect(min, max) && ImGui.IsMouseClicked(ImGuiMouseButton.Left);
	}
}
