using ImGuiNET;

using Ktisis.Scene.Editing;

namespace Ktisis.Common.Utility;

public static class GuiHelpers {
	public static void Spacing(int count) {
		for (var i = 0; i < count; i++)
			ImGui.Spacing();
	}

	public static SelectFlags GetSelectFlags() {
		var result = SelectFlags.None;
		if (ImGui.IsKeyDown(ImGuiKey.ModCtrl))
			result |= SelectFlags.Ctrl;
		// TODO: Shift
		return result;
	}
}
