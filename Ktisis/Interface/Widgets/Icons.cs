using System.Numerics;

using Dalamud.Interface;

using ImGuiNET;

namespace Ktisis.Interface.Widgets; 

internal class Icons {
	internal static void DrawIcon(FontAwesomeIcon icon, uint? color = null) {
		var hasColor = color.HasValue;
		if (hasColor) ImGui.PushStyleColor(ImGuiCol.Button, color!.Value);
		
		ImGui.PushFont(UiBuilder.IconFont);
		ImGui.Text(icon.ToIconString());
		ImGui.PopFont();

		if (hasColor) ImGui.PopStyleColor();
	}

	internal static Vector2 CalcIconSize(FontAwesomeIcon icon) {
		ImGui.PushFont(UiBuilder.IconFont);
		var result = ImGui.CalcTextSize(icon.ToIconString());
		ImGui.PopFont();
		return result;
	}
}