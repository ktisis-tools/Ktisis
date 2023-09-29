using System.Numerics;

using Dalamud.Interface;

using ImGuiNET;

using ImGuizmoNET;

using Ktisis.History;
using Ktisis.Interface.Components;
using Ktisis.Events;
using Ktisis.Interface.Windows.ActorEdit;
using Ktisis.Interop.Hooks;
using Ktisis.Overlay;
using Ktisis.Util;

namespace Ktisis.Interface.Windows.Toolbar {
	public static class ToolbarWindow {
		internal static bool Visible = false;

		// Toggle visibility
		public static void Toggle() => Visible = !Visible;

		public static void Init() {
			EventManager.OnGPoseChange += OnGPoseStateChange;
		}

		public static void Dispose() {
			EventManager.OnGPoseChange -= OnGPoseStateChange;
		}

		public static void OnGPoseStateChange(bool isInGPose) {
			if (isInGPose) {
				if (Ktisis.Configuration.ShowToolbar)
					Visible = true;
			} else
				Visible = false;
		}

		// Draw window
		public static void Draw() {
			if (!Visible)
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

			ControlButtons.DrawSimplePoseSwitch();

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

			ActorsList.DrawToolbar();

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

			DrawMainButtons();

			ImGui.SameLine(0, ImGui.GetFontSize());

			var parent = cfg.EnableParenting;
			ControlButtons.VerticalAlignTextOnButtonSize(0.9f);
			if (ImGui.Checkbox("Parent", ref parent))
				cfg.EnableParenting = parent;

			ImGui.SameLine(0, ImGui.GetFontSize());
			
			if (GuiHelpers.IconButtonTooltip(IconsPool.Import, "Import and Export pose and appearance", ControlButtons.ButtonSize))
				ImportExportWindow.Toggle();
			
			ImGui.SameLine();
			
			if (GuiHelpers.IconButtonTooltip(IconsPool.More, "Advanced tools window", ControlButtons.ButtonSize))
				AdvancedWindow.Toggle();

			ImGui.SameLine();

			if (GuiHelpers.IconButtonTooltip(IconsPool.Settings, "Ktisis main window", ControlButtons.ButtonSize))
				Workspace.Workspace.Toggle();
			ImGui.End();
		}

		public static void DrawMainButtons() {
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
