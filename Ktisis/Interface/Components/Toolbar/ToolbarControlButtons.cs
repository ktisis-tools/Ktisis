
using Dalamud.Interface;

using ImGuiNET;

using ImGuizmoNET;

using Ktisis.History;
using Ktisis.Interface.Windows.Toolbar;
using Ktisis.Overlay;
using Ktisis.Util;

namespace Ktisis.Interface.Components.Toolbar {
	public static class ToolbarControlButtons {

		public static void Draw() {
			// Operations
			ControlButtons.ButtonChangeOperation(OPERATION.TRANSLATE, IconsPool.Position);
			ImGui.SameLine();
			ControlButtons.ButtonChangeOperation(OPERATION.ROTATE, IconsPool.Rotation);
			ImGui.SameLine();
			ControlButtons.ButtonChangeOperation(OPERATION.SCALE, IconsPool.Scale);
			ImGui.SameLine();
			ControlButtons.VerticalAlignTextOnButtonSize(0.9f);
			if (GuiHelpers.IconButtonTooltip(IconsPool.DownMore, "Show transform table ")) {
				TransformWindow.Toggle();
			}
			ImGui.SameLine(0, ImGui.GetFontSize());
			ControlButtons.VerticalAlignTextOnButtonSize(0.9f);
			if (GuiHelpers.IconButtonTooltip(IconsPool.Undo, "Undo previous action")) {
				HistoryManager.Undo();
			}
			ImGui.SameLine();
			ControlButtons.VerticalAlignTextOnButtonSize(0.9f);
			if (GuiHelpers.IconButtonTooltip(IconsPool.Redo, "Redo previous action")) {
				HistoryManager.Redo();
			}
			ImGui.SameLine(0, ImGui.GetFontSize());
			// Extra Options
			var gizmoMode = Ktisis.Configuration.GizmoMode;
			if (GuiHelpers.IconButtonTooltip(gizmoMode == MODE.WORLD ? FontAwesomeIcon.Globe : FontAwesomeIcon.Home, "Local / World orientation mode switch.", ControlButtons.ButtonSize))
				Ktisis.Configuration.GizmoMode = gizmoMode == MODE.WORLD ? MODE.LOCAL : MODE.WORLD;

			ImGui.SameLine();

			var showSkeleton = Ktisis.Configuration.ShowSkeleton;
			if (showSkeleton) ImGui.PushStyleColor(ImGuiCol.Text, GuiHelpers.VisibleCheckmarkColor());
			if (GuiHelpers.IconButton(showSkeleton ? FontAwesomeIcon.Eye : FontAwesomeIcon.EyeSlash, ControlButtons.ButtonSize))
				Skeleton.Toggle();
			if (showSkeleton) ImGui.PopStyleColor();
			GuiHelpers.Tooltip((showSkeleton ? "Hide" : "Show") + " skeleton lines and bones.");

			ImGui.SameLine();

			ControlButtons.DrawSiblingLink();
		}

	}
}
