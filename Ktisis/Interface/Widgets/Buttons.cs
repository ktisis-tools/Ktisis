using System.Numerics;

using Dalamud.Interface;

using ImGuiNET;

namespace Ktisis.Interface.Widgets;

internal static class Buttons {
	internal static bool IsClicked() {
		// Special case for visibility button - ImGui does not detect it as hovered for some reason.
		var min = ImGui.GetItemRectMin();
		var max = ImGui.GetItemRectMax();
		return ImGui.IsMouseHoveringRect(min, max) && ImGui.IsMouseClicked(ImGuiMouseButton.Left);
	}

	internal static bool DrawIconButton(FontAwesomeIcon icon, Vector2? size = null) {
		var font = UiBuilder.IconFont;

		if (size == null) {
			var newSize = font.FontSize + ImGui.GetStyle().ItemInnerSpacing.X * 2;
			size = new Vector2(newSize, newSize);
		}

		ImGui.PushFont(font);
		var result = ImGui.Button(icon.ToIconString(), size.Value);
		ImGui.PopFont();

		return result;
	}

	internal static bool DrawIconButtonHint(FontAwesomeIcon icon, string hint, Vector2? size = null) {
		var result = DrawIconButton(icon, size);
		if (ImGui.IsItemHovered()) {
			ImGui.BeginTooltip();
			ImGui.Text(hint);
			ImGui.EndTooltip();
		}
		return result;
	}

	// https://github.com/goatcorp/Dalamud/blob/40b875c8e93b796eb9104233263bf6bd790afc6d/Dalamud/Interface/Components/ImGuiComponents.ToggleSwitch.cs#L10

	private readonly static Vector4 ToggleBg = new(0.35f, 0.35f, 0.35f, 1.0f);
	private readonly static Vector4 ToggleBgHover = new(0.78f, 0.78f, 0.78f, 1.0f);

	internal readonly static float ToggleWidthRatio = 1.55f;

	internal static bool ToggleButton(string id, ref bool v, uint circleColor = 0xFFFFFFFF) {
		var colors = ImGui.GetStyle().Colors;
		var p = ImGui.GetCursorScreenPos();
		var drawList = ImGui.GetWindowDrawList();

		var height = ImGui.GetFrameHeight();
		var width = height * ToggleWidthRatio;
		var radius = height * 0.50f;

		var changed = false;
		ImGui.InvisibleButton(id, new Vector2(width, height));
		if (ImGui.IsItemClicked()) {
			changed = true;
			v = !v;
		}

		var color = (hover: ImGui.IsItemHovered(), toggle: v) switch {
			(true, true) => ToggleBgHover,
			(false, true) => ToggleBg,
			(true, false) => colors[(int)ImGuiCol.ButtonActive],
			(false, false) => colors[(int)ImGuiCol.Button] * 0.6f
		};

		var p_max = new Vector2(p.X + width, p.Y + height);
		drawList.AddRectFilled(p, p_max, ImGui.GetColorU32(color), height * 0.5f);
		drawList.AddCircleFilled(new Vector2(p.X + radius + ((v ? 1 : 0) * (width - (radius * 2.0f))), p.Y + radius), radius - 1.5f, circleColor);

		return changed;
	}
}
