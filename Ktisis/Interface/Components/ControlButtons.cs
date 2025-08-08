using System;
using System.Linq;
using System.Numerics;

using Dalamud.Bindings.ImGui;
using Dalamud.Bindings.ImGuizmo;

using Dalamud.Interface;
using Dalamud.Interface.Components;

using Ktisis.Util;
using Ktisis.Overlay;
using Ktisis.Interop.Hooks;
using Ktisis.Structs.Bones;
using Ktisis.Interface.Windows;
using Ktisis.Interface.Windows.Workspace;
using Ktisis.History;

namespace Ktisis.Interface.Components {
	public static class ControlButtons {
		public static Vector2 ButtonSize => new Vector2(ImGui.GetFontSize() * 1.6f) + ImGui.GetStyle().FramePadding;
		private static bool IsSettingsHovered = false;
		private static bool IsSettingsActive = false;

		// utils
		public static void VerticalAlignTextOnButtonSize(float percentage = 0.667f) => ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (ButtonSize.Y / 2 - ImGui.GetFontSize() * percentage)); // align text with button size

		public static void DrawExtra() {
			var gizmode = Ktisis.Configuration.GizmoMode;
			if (GuiHelpers.IconButtonTooltip(gizmode == ImGuizmoMode.World ? FontAwesomeIcon.Globe : FontAwesomeIcon.Home, "Local / World orientation mode switch.", ButtonSize))
				Ktisis.Configuration.GizmoMode = gizmode == ImGuizmoMode.World ? ImGuizmoMode.Local : ImGuizmoMode.World;

			ImGui.SameLine();
			var showSkeleton = Ktisis.Configuration.ShowSkeleton;
			if (showSkeleton) ImGui.PushStyleColor(ImGuiCol.Text,GuiHelpers.VisibleCheckmarkColor());
			if (GuiHelpers.IconButton(showSkeleton ? FontAwesomeIcon.Eye : FontAwesomeIcon.EyeSlash, ButtonSize))
				Skeleton.Toggle();
			if (showSkeleton) ImGui.PopStyleColor();
			GuiHelpers.Tooltip((showSkeleton ? "Hide" : "Show") + " skeleton lines and bones.");


			ImGui.SameLine();
			DrawSiblingLink();

			ImGui.SameLine();

			var gizmoActive = OverlayWindow.IsGizmoVisible;
			if (!gizmoActive) ImGui.BeginDisabled();
			if (GuiHelpers.IconButtonTooltip(FontAwesomeIcon.MinusCircle, "Deselect gizmo", ButtonSize))
				OverlayWindow.DeselectGizmo();
			if (!gizmoActive) ImGui.EndDisabled();

			var canUndo = HistoryManager.CanUndo;
			var canRedo = HistoryManager.CanRedo;

			ImGui.SameLine();

			if (!canUndo) ImGui.BeginDisabled();
			if (GuiHelpers.IconButtonTooltip(FontAwesomeIcon.Backward, "Undo", ButtonSize)) 
				HistoryManager.Undo();
			if (!canUndo) ImGui.EndDisabled();

			ImGui.SameLine();

			if (!canRedo) ImGui.BeginDisabled();
			if (GuiHelpers.IconButtonTooltip(FontAwesomeIcon.Forward, "Redo", ButtonSize))
				HistoryManager.Redo();
			if (!canRedo) ImGui.EndDisabled();
		}

		// As these buttons are a bit special and should not be as present as others
		// we remove the border and change the hover behavior.

		private static void DrawInfo() {
			ImGui.PushStyleColor(ImGuiCol.Button, 0x00000000);
			ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 200f);
			ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(ImGui.GetFontSize() * 0.25f));

			if (GuiHelpers.IconButton(FontAwesomeIcon.InfoCircle, new(ImGui.GetFontSize() * 1.5f)))
				Information.Toggle();

			ImGui.PopStyleColor();
			ImGui.PopStyleVar(2);

			IsSettingsHovered = ImGui.IsItemHovered();
			IsSettingsActive = ImGui.IsItemActive();

			GuiHelpers.Tooltip("Information");
		}
		private static void DrawSettings() {
			ImGui.PushStyleColor(ImGuiCol.Button, 0x00000000);
			ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 200f);
			ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(ImGui.GetFontSize() * 0.25f));

			if (GuiHelpers.IconButton(FontAwesomeIcon.Cog, new(ImGui.GetFontSize() * 1.5f)))
				ConfigGui.Toggle();

			ImGui.PopStyleColor();
			ImGui.PopStyleVar(2);

			IsSettingsHovered = ImGui.IsItemHovered();
			IsSettingsActive = ImGui.IsItemActive();

			GuiHelpers.Tooltip("Open Settings");
		}
		public static void PlaceAndRenderSettings() {

			var initialPos = ImGui.GetCursorPos();
			ImGui.PushClipRect(ImGui.GetWindowPos(), ImGui.GetWindowPos() + ImGui.GetWindowSize(), false);

			// A bit complicated formulas to handle any styles values
			ImGui.SetCursorPosX(initialPos.X + ImGui.GetContentRegionAvail().X - ImGui.GetStyle().FramePadding.X - ImGui.GetFontSize() * (3.5f * 1.5f) - (float)Math.Exp(ImGui.GetFontSize() / 18));
			ImGui.SetCursorPosY(initialPos.Y - ImGui.GetStyle().FramePadding.Y - (float)Math.Log2(ImGui.GetTextLineHeight()) * 3.5f - ImGui.GetTextLineHeight()*1.05f);

			DrawInfo();
			ImGui.SameLine();
			DrawSettings();

			ImGui.PopClipRect();
			ImGui.SetCursorPos(initialPos);

		}

		public static void ButtonChangeOperation(ImGuizmoOperation operation, FontAwesomeIcon icon) {
			var isCurrentOperation = Ktisis.Configuration.GizmoOp.HasFlag(ImGuizmoOperation.RotateX) ? (Ktisis.Configuration.GizmoOp | ImGuizmoOperation.Rotate).HasFlag(operation) : Ktisis.Configuration.GizmoOp.HasFlag(operation);
			if (isCurrentOperation) ImGui.PushStyleColor(ImGuiCol.Text, GuiHelpers.VisibleCheckmarkColor());

			if (GuiHelpers.IconButton(icon, ButtonSize))
				if (!isCurrentOperation)
					Ktisis.Configuration.GizmoOp = ImGui.GetIO().KeyShift ? Ktisis.Configuration.GizmoOp.AddFlag(operation) : operation;
				else if (ImGui.GetIO().KeyCtrl)
					Ktisis.Configuration.GizmoOp = Ktisis.Configuration.GizmoOp.ToggleFlag(ImGuizmoOperation.Rotate, ImGuizmoOperation.RotateX, ImGuizmoOperation.RotateY, ImGuizmoOperation.RotateZ);
				else if (ImGui.GetIO().KeyShift)
					Ktisis.Configuration.GizmoOp = Ktisis.Configuration.GizmoOp.RemoveFlag(operation);
				else
					Ktisis.Configuration.GizmoOp = operation;
			
			if (isCurrentOperation) ImGui.PopStyleColor();

			string help = "";
			if (isCurrentOperation)
				help += "Current gizmo operation is ";
			else
				help += "Change gizmo operation to ";

			help += operation switch {
				ImGuizmoOperation.Translate => "Position",
				ImGuizmoOperation.Rotate => "Rotation",
				ImGuizmoOperation.Scale => "Scale",
				ImGuizmoOperation.Universal => "Universal",
				var _ => throw new ArgumentOutOfRangeException(nameof(operation), operation, null)
			};

			GuiHelpers.Tooltip(help + ".");
		}

		// Independant from the others
		public static void DrawPoseSwitch() {
			ImGui.SetCursorPosX(ImGui.CalcTextSize("GPose Disabled").X + (ImGui.GetFontSize() * 8) + ImGui.GetStyle().ItemSpacing.X + GuiHelpers.CalcIconSize(FontAwesomeIcon.Cog).X); // Prevents text overlap

			ImGui.BeginDisabled(!Ktisis.IsInGPose);
			var pose = PoseHooks.PosingEnabled;
			if (Ktisis.IsInGPose) ImGui.PushStyleColor(ImGuiCol.Text, pose ? Workspace.ColGreen : Workspace.ColRed);
			var label = pose ? "Posing" : "Not Posing";
			float toggleWidth = ImGui.GetFrameHeight() * 1.55f;
			float offsetWidth = GuiHelpers.GetRightOffset(toggleWidth);
			GuiHelpers.TextRight(label, offsetWidth);
			if (Ktisis.IsInGPose) ImGui.PopStyleColor();
			ImGui.SameLine();

			if (!Ktisis.IsInGPose)
				ImGuiComponents.DisabledToggleButton("Toggle Posing", false);
			else
				if (GuiHelpers.ToggleButton("Toggle Posing", ref pose, pose ? Workspace.ColGreen : Workspace.ColRed))
				PoseHooks.TogglePosing();

			ImGui.EndDisabled();
		}

		public static void DrawSimplePoseSwitch() {
			var pose = PoseHooks.PosingEnabled;
			if (!Ktisis.IsInGPose)
				ImGuiComponents.DisabledToggleButton("Toggle Posing", false);
			else if (GuiHelpers.ToggleButton("Toggle Posing", ref pose, pose ? Workspace.ColGreen : Workspace.ColRed))
				PoseHooks.TogglePosing();
			GuiHelpers.Tooltip("Toggle Posing");
		}

		public static void DrawSiblingLink() {
			var siblingLink = Ktisis.Configuration.SiblingLink;

			if (siblingLink != SiblingLink.None) ImGui.PushStyleColor(ImGuiCol.Text, GuiHelpers.VisibleCheckmarkColor());
			if (GuiHelpers.IconButton(SiblingLinkToIcon(siblingLink), ButtonSize))
				CircleTroughSiblingLinkModes();
			if (siblingLink != SiblingLink.None) ImGui.PopStyleColor();
			GuiHelpers.Tooltip(SiblingLinkToTooltip(siblingLink));
		}
		public static void CircleTroughSiblingLinkModes() =>
			Ktisis.Configuration.SiblingLink = Ktisis.Configuration.SiblingLink == Enum.GetValues(typeof(SiblingLink)).Cast<SiblingLink>().Last() ? SiblingLink.None : Ktisis.Configuration.SiblingLink + 1;
		private static FontAwesomeIcon SiblingLinkToIcon(SiblingLink siblingLink)
			=> siblingLink switch {
				SiblingLink.Rotation => FontAwesomeIcon.Link,
				SiblingLink.RotationMirrorX => FontAwesomeIcon.Adjust,
				_ => FontAwesomeIcon.ArrowUp
			};
		private static string SiblingLinkToTooltip(SiblingLink siblingLink)
			=> siblingLink switch {
				SiblingLink.Rotation => "Link rotation",
				SiblingLink.RotationMirrorX => "Mirror rotation",
				_ => "No sibling link"
			};
	}
}
