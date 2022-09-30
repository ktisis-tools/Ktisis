using System.Collections.Generic;

using ImGuiNET;

using Dalamud.Logging;
using Dalamud.Interface;
using Dalamud.Interface.Components;

using Ktisis.Structs.Actor;

namespace Ktisis.Interface.Windows.ActorEdit {
	public class EditGaze {
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

			if (ImGuiComponents.IconButton(IsLinked ? FontAwesomeIcon.Link : FontAwesomeIcon.Unlink))
				IsLinked = !IsLinked;

			var id = Target->ObjectID;
			if (!ActorControl.ContainsKey(id))
				ActorControl.Add(id, new ActorGaze());

			var gaze = ActorControl[id];

			var result = false;
			if (IsLinked) {
				result |= DrawGaze(ref gaze.Other, GazeControl.All);
			} else {
				result |= DrawGaze(ref gaze.Eyes, GazeControl.Eyes);
				result |= DrawGaze(ref gaze.Head, GazeControl.Head);
				result |= DrawGaze(ref gaze.Torso, GazeControl.Torso);
			}

			if (result)
				ActorControl[id] = gaze;

			ImGui.EndTabItem();
		}

		public static bool DrawGaze(ref Gaze gaze, GazeControl type) {
			var result = false;

			var enabled = gaze.Mode != 0;
			if (ImGui.Checkbox($"{type}", ref enabled)) {
				result = true;
				gaze.Mode = enabled ? GazeMode.Target : GazeMode.Disabled;
			}

			result |= ImGui.DragFloat3($"##{type}", ref gaze.Pos, 0.005f);

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