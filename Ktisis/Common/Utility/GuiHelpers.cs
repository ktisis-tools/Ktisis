using Dalamud.Bindings.ImGui;

using Ktisis.Editor.Selection;

namespace Ktisis.Common.Utility;

public static class GuiHelpers {
	public static SelectMode GetSelectMode() {
		var mode = SelectMode.Default;
		if (ImGui.IsKeyDown(ImGuiKey.ModCtrl))
			mode = SelectMode.Multiple;
		// TODO: Shift
		return mode;
	}
	
	public static float CalcContrastRatio(uint background, uint foreground) {
		// https://github.com/ocornut/imgui/issues/3798
		const float sr = 0.2126f / 255.0f;
		const float sg = 0.7152f / 255.0f;
		const float sb = 0.0722f / 255.0f;
		float sa0 = background >> 24 & 0xFF;
		float sa1 = foreground >> 24 & 0xFF;
		var contrastRatio =
			(sr * sa0 * (background >> 16 & 0xFF) +
			sg * sa0 * (background >> 8 & 0xFF) +
			sb * sa0 * (background >> 0 & 0xFF) + 0.05f) /
			(sr * sa1 * (foreground >> 16 & 0xFF) +
			sg * sa1 * (foreground >> 8 & 0xFF) +
			sb * sa1 * (foreground >> 0 & 0xFF) + 0.05f);
		if (contrastRatio < 1.0f)
			return 1.0f / contrastRatio;
		return contrastRatio;
	}

	public static uint CalcBlackWhiteTextColor(uint background) {
		const uint black = 0xFF000000;
		const uint white = 0xFFFFFFFF;
		return CalcContrastRatio(background, white) < 2.0f ? black : white;
	}
}
