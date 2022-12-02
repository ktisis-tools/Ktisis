using System;
using System.Linq;
using System.Numerics;

using Dalamud.Interface;
using Dalamud.Interface.Components;

using ImGuiNET;

using ImGuizmoNET;

using Ktisis.Interface.Windows.Toolbar;
using Ktisis.Interop.Hooks;
using Ktisis.Overlay;
using Ktisis.Structs.Bones;
using Ktisis.Util;

namespace Ktisis.Interface.Components.Toolbar {
	public static class ToolbarControlButtons {

		public static Vector2 ButtonSize = new Vector2(ImGui.GetFontSize() * 2.0f) + ImGui.GetStyle().FramePadding;
		public static void VerticalAlignTextOnButtonSize(float percentage = 0.667f) => ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (ButtonSize.Y / 2 - ImGui.GetFontSize() * percentage)); // align text with button size

		public static void Draw() {
			// Operations
			ButtonChangeOperation(OPERATION.TRANSLATE, IconsPool.Position);
			ImGui.SameLine();
			ButtonChangeOperation(OPERATION.ROTATE, IconsPool.Rotation);
			ImGui.SameLine();
			ButtonChangeOperation(OPERATION.SCALE, IconsPool.Scale);
			ImGui.SameLine();
			VerticalAlignTextOnButtonSize(0.9f);
			if (GuiHelpers.TextButtonTooltip("+", "Show transform table ")) {
				TransformWindow.Toggle();
			}
			ImGui.SameLine(0, ImGui.GetFontSize());

			// Extra Options
			var gizmoMode = Ktisis.Configuration.GizmoMode;
			if (GuiHelpers.IconButtonTooltip(gizmoMode == MODE.WORLD ? FontAwesomeIcon.Globe : FontAwesomeIcon.Home, "Local / World orientation mode switch.", ButtonSize))
				Ktisis.Configuration.GizmoMode = gizmoMode == MODE.WORLD ? MODE.LOCAL : MODE.WORLD;

			ImGui.SameLine();

			var showSkeleton = Ktisis.Configuration.ShowSkeleton;
			if (showSkeleton) ImGui.PushStyleColor(ImGuiCol.Text, ImGui.GetStyle().Colors[(int)ImGuiCol.CheckMark]);
			if (GuiHelpers.IconButton(showSkeleton ? FontAwesomeIcon.Eye : FontAwesomeIcon.EyeSlash, ButtonSize))
				Skeleton.Toggle();
			if (showSkeleton) ImGui.PopStyleColor();
			GuiHelpers.Tooltip((showSkeleton ? "Hide" : "Show") + " skeleton lines and bones.");

			ImGui.SameLine();

			DrawSiblingLink();
		}

		public static void ButtonChangeOperation(OPERATION operation, FontAwesomeIcon icon) {
			var isCurrentOperation = Ktisis.Configuration.GizmoOp.HasFlag(OPERATION.ROTATE_X) ? (Ktisis.Configuration.GizmoOp | OPERATION.ROTATE).HasFlag(operation) : Ktisis.Configuration.GizmoOp.HasFlag(operation);
			if (isCurrentOperation) ImGui.PushStyleColor(ImGuiCol.Text, ImGui.GetStyle().Colors[(int)ImGuiCol.CheckMark]);

			if (GuiHelpers.IconButton(icon, ButtonSize))
				if (!isCurrentOperation)
					Ktisis.Configuration.GizmoOp = ImGui.GetIO().KeyShift ? Ktisis.Configuration.GizmoOp.AddFlag(operation) : operation;
				else if (ImGui.GetIO().KeyCtrl)
					Ktisis.Configuration.GizmoOp = Ktisis.Configuration.GizmoOp.ToggleFlag(OPERATION.ROTATE, OPERATION.ROTATE_X, OPERATION.ROTATE_Y, OPERATION.ROTATE_Z);
				else if (ImGui.GetIO().KeyShift)
					Ktisis.Configuration.GizmoOp = Ktisis.Configuration.GizmoOp.RemoveFlag(operation);
				else
					Ktisis.Configuration.GizmoOp = operation;

			if (isCurrentOperation) ImGui.PopStyleColor();

			var help = "";
			if (isCurrentOperation)
				help += "Current gizmo operation is ";
			else
				help += "Change gizmo operation to ";

			help += operation switch {
				OPERATION.TRANSLATE => "Position",
				OPERATION.ROTATE => "Rotation",
				OPERATION.SCALE => "Scale",
				var _ => throw new ArgumentOutOfRangeException(nameof(operation), operation, null)
			};

			GuiHelpers.Tooltip(help + ".");
		}

		public static void DrawPoseSwitch() {
			var pose = PoseHooks.PosingEnabled;
			if (!Ktisis.IsInGPose)
				ImGuiComponents.DisabledToggleButton("Toggle Posing", false);
			else if (GuiHelpers.ToggleButton("Toggle Posing", ref pose, pose ? AdvancedWindow.ColGreen : AdvancedWindow.ColRed))
				PoseHooks.TogglePosing();
			GuiHelpers.Tooltip("Toggle Posing");
		}

		private static void DrawSiblingLink() {
			var siblingLink = Ktisis.Configuration.SiblingLink;

			if (siblingLink != SiblingLink.None) ImGui.PushStyleColor(ImGuiCol.Text, ImGui.GetStyle().Colors[(int)ImGuiCol.CheckMark]);
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