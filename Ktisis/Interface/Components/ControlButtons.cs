using System.Numerics;

using ImGuiNET;
using ImGuizmoNET;

using Dalamud.Interface;

using Ktisis.Interface.Windows;
using Ktisis.Overlay;
using Ktisis.Util;
using Ktisis.Interface.Windows.ActorEdit;

namespace Ktisis.Interface.Components {
	public static class ControlButtons {
		public static Vector2 ButtonSize = new Vector2(ImGui.GetFontSize() * 1.75f);
		private static bool IsSettingsHovered = false;
		private static bool IsSettingsActive = false;

		// utils
		public static void AlignTextOnButtonSize(float percentage = 0.75f) => ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (ButtonSize.Y / 2 - ImGui.GetFontSize() * percentage)); // align text with button size

		public static void Draw() {
			DrawGizmoOperations();

			DrawExtra();
			ImGui.SameLine();
			DrawSettings();
		}

		private static void DrawGizmoOperations() {
			ButtonChangeOperation(OPERATION.TRANSLATE, FontAwesomeIcon.LocationArrow);
			ImGui.SameLine();
			ButtonChangeOperation(OPERATION.ROTATE, FontAwesomeIcon.Sync);
			ImGui.SameLine();
			ButtonChangeOperation(OPERATION.SCALE, FontAwesomeIcon.ExpandAlt);
			ImGui.SameLine();
			ButtonChangeOperation(OPERATION.UNIVERSAL, FontAwesomeIcon.DotCircle);
		}
		private static void DrawExtra() {
			var gizmode = Ktisis.Configuration.GizmoMode;
			if (GuiHelpers.IconButtonTooltip(gizmode == MODE.WORLD ? FontAwesomeIcon.Globe : FontAwesomeIcon.Home, "Local / World orientation mode switch.", ButtonSize))
				Ktisis.Configuration.GizmoMode = gizmode == MODE.WORLD ? MODE.LOCAL : MODE.WORLD;

			ImGui.SameLine();
			var showSkeleton = Ktisis.Configuration.ShowSkeleton;
			if (showSkeleton) ImGui.PushStyleColor(ImGuiCol.Text, ImGui.GetStyle().Colors[(int)ImGuiCol.CheckMark]);
			if (GuiHelpers.IconButton(showSkeleton ? FontAwesomeIcon.Eye : FontAwesomeIcon.EyeSlash, ButtonSize))
				Skeleton.Toggle();
			if (showSkeleton) ImGui.PopStyleColor();
			GuiHelpers.Tooltip((showSkeleton ? "Hide" : "Show") + " skeleton lines and bones.");
		}

		// As the settings button is a bit special and should not be as present as others
		// we remove the border and change the hover behavior.
		private static void DrawSettings() {
			GuiHelpers.TextRight("", GuiHelpers.GetRightOffset(GuiHelpers.CalcIconSize(FontAwesomeIcon.Cog).X) + (ImGui.GetStyle().ItemSpacing.X * 1.5f));

			ImGui.SameLine();
			AlignTextOnButtonSize(0.5f); // align text with button size
			var buttonColor = !IsSettingsHovered ? ImGuiCol.TextDisabled : (IsSettingsActive ? ImGuiCol.ButtonActive : ImGuiCol.Text);
			ImGui.PushStyleColor(ImGuiCol.Text, ImGui.GetStyle().Colors[(int)(buttonColor)]);
			ImGui.PushStyleColor(ImGuiCol.Button, 0x00000000);
			ImGui.PushStyleColor(ImGuiCol.ButtonActive, 0x00000000);
			ImGui.PushStyleColor(ImGuiCol.ButtonHovered, 0x00000000);
			ImGui.PushStyleVar(ImGuiStyleVar.FrameBorderSize, 0f);

			if (GuiHelpers.IconButton(FontAwesomeIcon.Cog))
				if (ConfigGui.Visible) ConfigGui.Hide();
				else ConfigGui.Show();

			ImGui.PopStyleColor(4);
			ImGui.PopStyleVar();

			IsSettingsHovered = ImGui.IsItemHovered();
			IsSettingsActive = ImGui.IsItemActive();

			GuiHelpers.Tooltip("Open Settings.");
		}

		private static void ButtonChangeOperation(OPERATION operation, FontAwesomeIcon icon) {
			var isCurrentOperation = Ktisis.Configuration.GizmoOp == operation;
			if (isCurrentOperation) ImGui.PushStyleColor(ImGuiCol.Text, ImGui.GetStyle().Colors[(int)ImGuiCol.CheckMark]);
			if (GuiHelpers.IconButton(icon, ButtonSize))
				Ktisis.Configuration.GizmoOp = operation;
			if (isCurrentOperation) ImGui.PopStyleColor();

			string help = "";
			if (isCurrentOperation)
				help += "Current gizmo operation is ";
			else
				help += "Change gizmo operation to ";

			if (operation == OPERATION.TRANSLATE) help += "Position";
			if (operation == OPERATION.ROTATE) help += "Rotation";
			if (operation == OPERATION.SCALE) help += "Scale";
			if (operation == OPERATION.UNIVERSAL) help += "Universal";

			GuiHelpers.Tooltip(help + ".");
		}
	}
}