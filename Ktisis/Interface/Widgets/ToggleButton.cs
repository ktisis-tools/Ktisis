using System.Numerics;

using ImGuiNET;

namespace Ktisis.Interface.Widgets;

public static class ToggleButton {
	// https://github.com/goatcorp/Dalamud/blob/40b875c8e93b796eb9104233263bf6bd790afc6d/Dalamud/Interface/Components/ImGuiComponents.ToggleSwitch.cs#L10

	private static readonly Vector4 ToggleBg = new(0.35f, 0.35f, 0.35f, 1.0f);
	private static readonly Vector4 ToggleBgHover = new(0.78f, 0.78f, 0.78f, 1.0f);

	private const float ToggleWidthRatio = 1.55f;

	public static bool Draw(string id, ref bool v, uint circleColor = 0xFFFFFFFF) {
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
