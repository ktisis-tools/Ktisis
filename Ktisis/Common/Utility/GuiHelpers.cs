using ImGuiNET;

using Ktisis.Editor.Selection;

namespace Ktisis.Common.Utility;

public static class GuiHelpers {
	public static SelectMode GetSelectMode() {
		var mode = SelectMode.Default;
		if (ImGui.IsKeyDown(ImGuiKey.ModCtrl))
			mode = SelectMode.Multiple;
		// TODO: Shift
		return mode;
	}
}
