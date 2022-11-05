using System.Numerics;

using ImGuiNET;
using ImGuizmoNET;

using Dalamud.Interface;
using Dalamud.Interface.Components;

using Ktisis.Interface.Windows;
using Ktisis.Interop;
using Ktisis.Overlay;
using Ktisis.Util;
using Dalamud.Game.ClientState.Objects.Types;

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
			GuiHelpers.TextRight("", GuiHelpers.GetRightOffset(GuiHelpers.CalcIconSize(FontAwesomeIcon.Cog).X) + (ImGui.GetStyle().FramePadding.X * 2f));

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

		// Independant from the others
		public static void DrawPoseSwitch() {
			ImGui.SetCursorPosX(ImGui.CalcTextSize("GPose Disabled").X + (ImGui.GetFontSize() * 8)); // Prevents text overlap

			ImGui.BeginDisabled(!Ktisis.IsInGPose);
			var pose = PoseHooks.PosingEnabled;
			if (Ktisis.IsInGPose) ImGui.PushStyleColor(ImGuiCol.Text, pose ? Workspace.ColGreen : Workspace.ColRed);
			var label = pose ? "Posing" : "Not Posing";
			float toggleWidth = ImGui.GetFrameHeight() * 1.55f;
			float offsetWidth = GuiHelpers.GetRightOffset(toggleWidth);
			GuiHelpers.TextRight(label, offsetWidth);
			if (Ktisis.IsInGPose) ImGui.PopStyleColor();
			ImGui.SameLine();
			var togglePos = ImGui.GetCursorPos(); // keep these coordinate as a starting point for GameAnimationIndicator

			if (!Ktisis.IsInGPose)
				ImGuiComponents.DisabledToggleButton("Toggle Posing", false);
			else
				if (GuiHelpers.ToggleButton("Toggle Posing", ref pose, pose ? Workspace.ColGreen : Workspace.ColRed))
				PoseHooks.TogglePosing();

			if (!Ktisis.IsInGPose && PoseHooks.PosingEnabled)
				PoseHooks.DisablePosing();

			ImGui.EndDisabled();


			// prints game indicator at the previously saved position togglePos
			var curCur = ImGui.GetCursorPos();
			ImGui.SetCursorPos(togglePos);
			GameAnimationIndicator(Ktisis.GPoseTarget);
			ImGui.SetCursorPos(curCur); // restore cursor
		}

		private static unsafe void GameAnimationIndicator(GameObject? target) {
			if (target == null) return;

			var isGamePlaybackRunning = PoseHooks.IsGamePlaybackRunning(target);

			ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (ImGui.GetFontSize() * 1.5f));
			ImGui.SetCursorPosX(ImGui.GetCursorPosX() + (ImGui.GetFontSize() * (isGamePlaybackRunning? 0.25f : 0.85f)));

			ImGui.PushStyleColor(ImGuiCol.Text, isGamePlaybackRunning ? Workspace.ColRed : Workspace.ColGreen);
			GuiHelpers.Icon(isGamePlaybackRunning ? FontAwesomeIcon.Play : FontAwesomeIcon.Pause);
			ImGui.PopStyleColor();
			GuiHelpers.Tooltip(isGamePlaybackRunning ? "Game Animation is playing for this target.\nPosing may reset periodically." : "Game Animation is paused for this target.");
		}
	}
}