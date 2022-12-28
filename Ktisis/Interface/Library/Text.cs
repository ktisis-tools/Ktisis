using ImGuiNET;

namespace Ktisis.Interface.Widgets {
	internal class Text {
		internal static void LabelRight(string text, float offset = 0) {
			offset = ImGui.GetContentRegionAvail().X - offset - ImGui.CalcTextSize(text).X;
			ImGui.SetCursorPosX(ImGui.GetCursorPosX() + offset);
			ImGui.TextUnformatted(text);
		}

		internal static void Tooltip(string text) {
			if (!ImGui.IsItemHovered()) return;

			ImGui.BeginTooltip();
			ImGui.PushTextWrapPos(ImGui.GetFontSize() * 35f);
			ImGui.TextUnformatted(text);
			ImGui.PopTextWrapPos();
			ImGui.EndTooltip();
		}
	}
}