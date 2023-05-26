using Dalamud.Interface;

using ImGuiNET;

using Ktisis.Camera;
using Ktisis.Util;

namespace Ktisis.Interface.Windows.Workspace.Tabs {
	public static class CameraTab {
		public static void Draw() {
			DrawCameraSelect();
			
			ImGui.EndTabItem();
		}

		private static void DrawCameraSelect() {
			var avail = ImGui.GetContentRegionAvail().X;
			var style = ImGui.GetStyle();

			var plusSize = GuiHelpers.CalcIconSize(FontAwesomeIcon.Plus);
			var camSize = GuiHelpers.CalcIconSize(FontAwesomeIcon.Camera);

			var comboWidth = avail - (style.ItemSpacing.X * 4) + style.ItemInnerSpacing.X - plusSize.X - camSize.X;
			ImGui.SetNextItemWidth(comboWidth);
			if (ImGui.BeginCombo("##Camera", "GPose Camera")) {
				ImGui.EndCombo();
			}

			ImGui.SameLine(ImGui.GetCursorPosX() + comboWidth + style.ItemInnerSpacing.X);
			GuiHelpers.IconButtonTooltip(FontAwesomeIcon.Plus, "Create new camera");

			ImGui.SameLine();
			if (GuiHelpers.IconButtonTooltip(FontAwesomeIcon.Camera, "Toggle work camera"))
				WorkCamera.Toggle();
		}
	}
}