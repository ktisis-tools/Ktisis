using ImGuiNET;

namespace Ktisis.Interface.Widgets {
	internal class Align {
		internal static float GetRightOffset(float size)
			=> size + ImGui.GetStyle().ItemSpacing.X;
	}
}
