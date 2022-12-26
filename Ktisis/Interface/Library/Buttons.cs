using System.Numerics;

using Dalamud.Interface;

using ImGuiNET;

namespace Ktisis.Interface.Library {
	internal class Buttons {
		internal static bool IconButton(FontAwesomeIcon icon, Vector2 size = default, string id = "") {
			ImGui.PushFont(UiBuilder.IconFont);
			var clicked = ImGui.Button($"{icon.ToIconString()}##{id}", size);
			ImGui.PopFont();
			return clicked;
		}

		internal static bool IconButtonTooltip(FontAwesomeIcon icon, string tooltip, Vector2 size = default, string id = "") {
			var clicked = IconButton(icon, size, id);
			Text.Tooltip(tooltip);
			return clicked;
		}

		internal static bool ToggleButton(string id, ref bool v, Vector4 circleColor) {
			var colors = ImGui.GetStyle().Colors;

			var cursorScreenPos = ImGui.GetCursorScreenPos();
			var windowDrawList = ImGui.GetWindowDrawList();

			float frameHeight = ImGui.GetFrameHeight();
			float num = frameHeight * 1.55f;
			float num2 = frameHeight * 0.5f;

			bool result = false;
			ImGui.InvisibleButton(id, new Vector2(num, frameHeight));
			if (ImGui.IsItemClicked()) {
				v = !v;
				result = true;
			}

			if (ImGui.IsItemHovered()) {
				windowDrawList.AddRectFilled(cursorScreenPos, new Vector2(cursorScreenPos.X + num, cursorScreenPos.Y + frameHeight), ImGui.GetColorU32(!v ? colors[(int)ImGuiCol.ButtonActive] : new Vector4(0.78f, 0.78f, 0.78f, 1f)), frameHeight * 0.5f);
			} else {
				windowDrawList.AddRectFilled(cursorScreenPos, new Vector2(cursorScreenPos.X + num, cursorScreenPos.Y + frameHeight), ImGui.GetColorU32(!v ? new Vector4(0.78f, 0.78f, 0.78f, 0.2f) : new Vector4(0.35f, 0.35f, 0.35f, 1f)), frameHeight * 0.5f);
			}

			windowDrawList.AddCircleFilled(new Vector2(cursorScreenPos.X + num2 + (v ? 1f : 0f) * (num - num2 * 2f), cursorScreenPos.Y + num2), num2 - 1.5f, ImGui.ColorConvertFloat4ToU32(circleColor));
			return result;
		}
	}
}
