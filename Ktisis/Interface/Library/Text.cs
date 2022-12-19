using ImGuiNET;

namespace Ktisis.Interface.Library {
	internal class Text {
		internal static void LabelRight(string text, float offset = 0) {
			offset = ImGui.GetContentRegionAvail().X - offset - ImGui.CalcTextSize(text).X;
			ImGui.SetCursorPosX(ImGui.GetCursorPosX() + offset);
			ImGui.TextUnformatted(text);
		}
	}
}
