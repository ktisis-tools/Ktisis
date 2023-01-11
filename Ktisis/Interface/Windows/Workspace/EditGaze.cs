using System.Collections.Generic;

using ImGuiNET;
using ImGuizmoNET;

using Dalamud.Interface;
using Dalamud.Interface.Components;

using Ktisis.Util;
using Ktisis.Overlay;
using Ktisis.Structs.Actor;
using Ktisis.Structs.Extensions;

namespace Ktisis.Interface.Windows.Workspace {
	public static class EditGaze {
		public static Dictionary<byte, ActorGaze>? ActorControl = null; // ObjectID : ActorGaze

		public static bool IsLinked {
			get => Ktisis.Configuration.LinkedGaze;
			set => Ktisis.Configuration.LinkedGaze = value;
		}

		public const uint UsingColor = 0xffde851f;

		// UI Code

		public unsafe static void Draw(Actor* target) {
			if (ActorControl == null)
				ActorControl = new();

			var id = target->ObjectID;
			if (!ActorControl.ContainsKey(id))
				ActorControl.Add(id, new ActorGaze());

			var gaze = ActorControl[id];

			var result = false;

			if (ImGuiComponents.IconButton(IsLinked ? FontAwesomeIcon.Link : FontAwesomeIcon.Unlink)) {
				if (IsLinked) {
					var move = gaze.Other;
					if (move.Mode != 0) {
						result = true;
						gaze.Head = move;
						gaze.Eyes = move;
						gaze.Torso = move;
						gaze.Other.Mode = GazeMode.Disabled;
					}
				}
				IsLinked = !IsLinked;
			}
			ImGui.SameLine();
			ImGui.Text(IsLinked ? "Linked" : "Unlinked");

			ImGui.Spacing();

			if (IsLinked) {
				result |= DrawGaze(target, ref gaze.Other, GazeControl.All);
			} else {
				result |= DrawGaze(target, ref gaze.Eyes, GazeControl.Eyes);
				ImGui.Spacing();
				result |= DrawGaze(target, ref gaze.Head, GazeControl.Head);
				ImGui.Spacing();
				result |= DrawGaze(target, ref gaze.Torso, GazeControl.Torso);
			}

			if (result)
				ActorControl[id] = gaze;
		}

		public unsafe static bool DrawGaze(Actor* target, ref Gaze gaze, GazeControl type) {
			var result = false;

			var isTracking = gaze.Mode == GazeMode._KtisisFollowCam_;

			var enabled = gaze.Mode != 0;
			if (ImGui.Checkbox($"{type}", ref enabled)) {
				result = true;
				gaze.Mode = enabled ? GazeMode.Target : GazeMode.Disabled;
			}

			// Gizmo controls
			// TODO: rotation mode.

			var gizmoId = $"edit_gaze_{type}";
			var gizmo = OverlayWindow.GetGizmo(gizmoId);

			var hasGizmo = gizmo != null;

			float buttonsWidth = GuiHelpers.CalcIconSize(FontAwesomeIcon.LocationArrow).X + GuiHelpers.CalcIconSize(FontAwesomeIcon.Eye).X + (ImGui.GetStyle().FramePadding.X * 4) + (ImGui.GetStyle().ItemSpacing.X * 2);
			ImGui.SameLine(ImGui.GetContentRegionAvail().X - GuiHelpers.WidthMargin() - buttonsWidth);

			if (isTracking) ImGui.BeginDisabled();
			else if (hasGizmo) ImGui.PushStyleColor(ImGuiCol.Button, UsingColor);

			if (ImGuiComponents.IconButton($"{FontAwesomeExtensions.ToIconChar(FontAwesomeIcon.LocationArrow)}##{type}")) {
				// Toggle gizmo on or off.
				// TODO: Place gizmo closer to character/camera.
				if (!enabled) { // Enable override if not already.
					result = true;
					enabled = true;
					gaze.Mode = GazeMode.Target;
				}

				if (gizmo == null)
					OverlayWindow.SetGizmoOwner(gizmoId);
				else
					OverlayWindow.DeselectGizmo();
			}

			if (isTracking) ImGui.EndDisabled();
			else if (hasGizmo) ImGui.PopStyleColor();

			// Draw gizmo

			if (gizmo != null) {
				if (enabled) {
					gizmo.ForceOp = OPERATION.TRANSLATE;
					result |= gizmo.Draw(ref gaze.Pos);
				} else {
					OverlayWindow.DeselectGizmo();
				}
			}

			// Camera tracking

			ImGui.SameLine();
			if (isTracking) ImGui.PushStyleColor(ImGuiCol.Button, UsingColor);
			if (ImGuiComponents.IconButton($"{FontAwesomeExtensions.ToIconChar(FontAwesomeIcon.Eye)}##{type}")) {
				result = true;
				enabled = true;
				gaze.Mode = isTracking ? GazeMode.Target : GazeMode._KtisisFollowCam_;
				if (hasGizmo) OverlayWindow.DeselectGizmo();
			}
			if (isTracking) ImGui.PopStyleColor();

			// Position

			if (type != GazeControl.All) {
				// If this gaze type is not being overwritten, copy the vanilla values.
				var baseGaze = target->Gaze[type];
				if (baseGaze.Mode != 0 && !enabled && !result)
					gaze.Pos = baseGaze.Pos;
			}

			ImGui.PushItemWidth(ImGui.GetContentRegionAvail().X - GuiHelpers.WidthMargin() - ImGui.GetStyle().ItemSpacing.X);
			result |= ImGui.DragFloat3($"##{type}", ref gaze.Pos, 0.005f);
			ImGui.PopItemWidth();

			return result;
		}

		// ControlGaze Hook

		public unsafe static void Apply(Actor* actor) {
			var isValid = ActorControl != null;
			if (Ktisis.IsInGPose && isValid) {
				var id = actor->ObjectID;
				if (ActorControl!.ContainsKey(id)) {
					var gaze = ActorControl[id];

					for (var i = -1; i < 3; i++) {
						var type = (GazeControl)i;

						var ctrl = gaze[type];
						if (ctrl.Mode != 0) {
							if (ctrl.Mode == GazeMode._KtisisFollowCam_) {
								var camera = Services.Camera->Camera;

								ctrl.Pos = camera->GetCameraPos();
								gaze[type] = ctrl;
								ActorControl[id] = gaze;

								ctrl.Mode = GazeMode.Target;
							}

							actor->LookAt(&ctrl, type);

							if (type == GazeControl.All)
								break;
						}
					}
				}
			} else if (isValid) {
				ActorControl = null;
			}
		}
	}
}
