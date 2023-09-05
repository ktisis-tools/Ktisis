using ImGuiNET;

using Ktisis.Scene.Editing;

namespace Ktisis.Interface.Helpers; 

public static class GuiSelect {
	public static SelectFlags GetSelectFlags() {
		var result = SelectFlags.None;
		if (ImGui.IsKeyDown(ImGuiKey.ModCtrl))
			result |= SelectFlags.Multiple;
		// TODO: Shift
		return result;
	}
}