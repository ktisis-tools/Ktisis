using System.Numerics;

using ImGuiNET;

using Dalamud.Interface;

namespace Ktisis.Interface.Library {
	internal class Icons {
		internal static void DrawIcon(FontAwesomeIcon icon, bool enabled = true, Vector4? color = null) {
			string iconText = icon.ToIconString() ?? "";

			var num = 0;
			if (color.HasValue) {
				ImGui.PushStyleColor(ImGuiCol.Button, color.Value);
				num++;
			}

			ImGui.PushFont(UiBuilder.IconFont);
			if (enabled)
				ImGui.Text(iconText);
			else
				ImGui.TextDisabled(iconText);
			ImGui.PopFont();

			if (num > 0)
				ImGui.PopStyleColor(num);
		}

		internal static Vector2 CalcIconSize(FontAwesomeIcon icon) {
			ImGui.PushFont(UiBuilder.IconFont);
			var size = ImGui.CalcTextSize(icon.ToIconString());
			ImGui.PopFont();
			return size;
		}
	}
}
