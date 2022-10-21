using System.Numerics;
using System.Collections.Generic;

using ImGuiNET;

using Dalamud.Interface;
using Dalamud.Interface.Components;

using Ktisis.Overlay;
using Ktisis.Structs.Actor;

namespace Ktisis.Interface.Windows.ActorEdit {
	public static class EditGaze {
		public unsafe static Actor* Target => EditActor.Target;

		public static Dictionary<byte, ActorGaze>? ActorControl = null; // ObjectID : ActorGaze

		public static bool IsLinked {
			get => Ktisis.Configuration.LinkedGaze;
			set => Ktisis.Configuration.LinkedGaze = value;
		}

		// UI Code

		public unsafe static void Draw() {
			if (ActorControl == null)
				ActorControl = new();

			var id = Target->ObjectID;
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
				result |= DrawGaze(ref gaze.Other, GazeControl.All);
			} else {
				result |= DrawGaze(ref gaze.Eyes, GazeControl.Eyes);
				ImGui.Spacing();
				result |= DrawGaze(ref gaze.Head, GazeControl.Head);
				ImGui.Spacing();
				result |= DrawGaze(ref gaze.Torso, GazeControl.Torso);
			}

			if (result)
				ActorControl[id] = gaze;

			ImGui.EndTabItem();
		}

		public unsafe static bool DrawGaze(ref Gaze gaze, GazeControl type) {
			var result = false;

			var enabled = gaze.Mode != 0;
			if (ImGui.Checkbox($"{type}", ref enabled)) {
				result = true;
				gaze.Mode = enabled ? GazeMode.Target : GazeMode.Disabled;
			}

			if (type != GazeControl.All) {
				// If this gaze type is not being overwritten, copy values from the vanilla system.
				var baseGaze = Target->Gaze.Get(type);
				if (baseGaze.Mode != 0 && (!enabled || result))
					gaze.Pos = baseGaze.Pos;
			}

			result |= ImGui.DragFloat3($"##{type}", ref gaze.Pos, 0.005f);

			// Gizmo controls
			// TODO: rotation mode.

			var gizmoId = $"edit_gaze_{type}";
			var gizmo = OverlayWindow.GetGizmo(gizmoId);

			ImGui.SameLine();
			if (ImGuiComponents.IconButton($"{FontAwesomeExtensions.ToIconChar(FontAwesomeIcon.EllipsisH)}##{type}")) {
				// Toggle gizmo on or off.
				// TODO: Place gizmo closer to character/camera.
				if (!enabled) { // Enable override if not already.
					result = true;
					enabled = true;
					gaze.Mode = GazeMode.Target;
				}
				gizmo = OverlayWindow.SetGizmoOwner(gizmo == null ? gizmoId : null);
			}

			if (gizmo != null) {
				if (enabled) {
					gizmo.Draw(ref gaze.Pos);
					result = true;
				} else {
					OverlayWindow.SetGizmoOwner(null);
				}
			}

			return result;
		}

		// ControlGaze Hook

		public unsafe static void Apply(Actor* actor) {
			var isValid = ActorControl != null;
			if (Ktisis.IsInGPose && isValid) {
				var id = actor->ObjectID;
				if (ActorControl!.ContainsKey(id)) {
					var gaze = ActorControl[id];
					if (gaze.Other.Mode != 0) {
						actor->LookAt(&gaze.Other, GazeControl.All);
					} else {
						if (gaze.Torso.Mode != 0)
							actor->LookAt(&gaze.Torso, GazeControl.Torso);
						if (gaze.Head.Mode != 0)
							actor->LookAt(&gaze.Head, GazeControl.Head);
						if (gaze.Eyes.Mode != 0)
							actor->LookAt(&gaze.Eyes, GazeControl.Eyes);
					}
				}
			} else if (isValid) {
				ActorControl = null;
			}
		}
	}
}