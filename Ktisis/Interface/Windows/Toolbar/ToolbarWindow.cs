using System.Numerics;

using ImGuiNET;

using Ktisis.Interface.Components.Toolbar;
using Ktisis.Interface.Windows.ActorEdit;
using Ktisis.Interop.Hooks;
using Ktisis.Overlay;
using Ktisis.Util;

namespace Ktisis.Interface.Windows.Toolbar {
	public static class ToolbarWindow {
		private static bool Visible = true;

		// Toggle visibility
		public static void Toggle() => Visible = !Visible;

		// Draw window
		public static void Draw() {
			if (!Visible || !Ktisis.IsInGPose || !Ktisis.Configuration.ShowToolbar)
				return;
			
			AdvancedWindow.Draw();
			TransformWindow.Draw();
			BonesWindow.Draw();
			ImportExportWindow.Draw();
			
			var cfg = Ktisis.Configuration;
			var size = new Vector2(-1, -1);
			var select = Skeleton.BoneSelect;
			var bone = Skeleton.GetSelectedBone();

			ImGui.SetNextWindowSize(size, ImGuiCond.FirstUseEver);

			if (!ImGui.Begin("Toolbar", ref Visible, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.AlwaysAutoResize)) {
				ImGui.End();
				return;
			}

			ToolbarControlButtons.DrawPoseSwitch();

			ImGui.SameLine();

			if (PoseHooks.AnamPosingEnabled) {
				Vector2 cursorScreenPos = ImGui.GetCursorScreenPos();
				ImDrawListPtr windowDrawList = ImGui.GetWindowDrawList();
				var frameHeight = ImGui.GetFrameHeight();
				var center = new Vector2(cursorScreenPos.X + ImGui.GetFontSize() / 2 + ImGui.GetStyle().ItemSpacing.X / 2, cursorScreenPos.Y + frameHeight / 2);
				windowDrawList.AddCircleFilled(
					center,
					ImGui.GetFontSize()/4,
					ImGui.GetColorU32(Workspace.Workspace.ColYellow)
				);

				var mousePos = ImGui.GetMousePos();
				if (Vector2.Distance(mousePos, center) < ImGui.GetFontSize()/1.2) {
					ImGui.BeginTooltip();
					ImGui.PushTextWrapPos(ImGui.GetFontSize() * 35.0f);
					ImGui.TextUnformatted("Anamnesis Enabled");
					ImGui.PopTextWrapPos();
					ImGui.EndTooltip();
				}
			}
			
			ImGui.SameLine(0, ImGui.GetFontSize() * (PoseHooks.AnamPosingEnabled ? 2 : 1));
			
			if (GuiHelpers.IconButtonTooltip(IconsPool.UserEdit, "Edit current Actor"))
				if (EditActor.Visible) EditActor.Hide();
				else EditActor.Show();

			ImGui.SameLine();

			ToolbarActorsList.Draw();

			ImGui.SameLine(0, ImGui.GetFontSize());

			var gizmoActive = OverlayWindow.IsGizmoVisible;
			if (!gizmoActive) ImGui.BeginDisabled();
			if (GuiHelpers.IconButtonTooltip(IconsPool.Deselect, "Deselect Gizmo")) {
				OverlayWindow.DeselectGizmo();
			}
			if (!gizmoActive) ImGui.EndDisabled();
			ImGui.SameLine();
			if (GuiHelpers.IconButtonTooltip(IconsPool.BoneList, "Bones Window"))
				BonesWindow.Toggle();
			
			ImGui.SameLine();
			
			if (select.Active && bone != null) {
				ImGui.Text($"{bone.LocaleName}");
			} else {
				ImGui.BeginDisabled();
				ImGui.Text("No bones selected");
				ImGui.EndDisabled();
			}

			// ----------------------------------------------------------------------------
			// ----------------------------------------------------------------------------
			// -------- SECOND LINE -------------------------------------------------------
			// ----------------------------------------------------------------------------
			// ----------------------------------------------------------------------------

			ToolbarControlButtons.Draw();

			ImGui.SameLine(0, ImGui.GetFontSize());

			var parent = cfg.EnableParenting;
			ToolbarControlButtons.VerticalAlignTextOnButtonSize(0.9f);
			if (ImGui.Checkbox("Parent", ref parent))
				cfg.EnableParenting = parent;

			ImGui.SameLine(0, ImGui.GetFontSize());
			
			if (GuiHelpers.IconButtonTooltip(IconsPool.Import, "Import and Export pose and appearance", ToolbarControlButtons.ButtonSize))
				ImportExportWindow.Toggle();
			
			ImGui.SameLine();
			
			if (GuiHelpers.IconButtonTooltip(IconsPool.More, "Advanced tools window", ToolbarControlButtons.ButtonSize))
				AdvancedWindow.Toggle();

			ImGui.SameLine();

			if (GuiHelpers.IconButtonTooltip(IconsPool.Settings, "Ktisis main window", ToolbarControlButtons.ButtonSize))
				Workspace.Workspace.Toggle();
			ImGui.End();
		}
	}
}