using ImGuiNET;

namespace Ktisis.Interface.Library {
	internal class Align {
		internal static float WidthMargin => Ktisis.Configuration.CustomWidthMarginDebug;

		internal static float GetRightOffset(float size)
			=> size + ImGui.GetStyle().ItemSpacing.X + WidthMargin;
	}
}
