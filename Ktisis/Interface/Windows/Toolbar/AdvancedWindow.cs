using System.Numerics;

using Dalamud.Interface;

using ImGuiNET;

using Ktisis.Interface.Components;
using Ktisis.Interface.Windows.Workspace;
using Ktisis.Interop.Hooks;
using Ktisis.Overlay;
using Ktisis.Structs.Actor;
using Ktisis.Structs.Poses;
using Ktisis.Util;

namespace Ktisis.Interface.Windows.Toolbar {
	public static class AdvancedWindow {
		private static bool Visible = false;

		public static Vector4 ColGreen = new(0, 255, 0, 255);
		public static Vector4 ColRed = new(255, 0, 0, 255);

		public static TransformTable Transform = new();

		// Toggle visibility
		public static void Toggle() => Visible = !Visible;

		// Draw window
		public unsafe static void Draw() {
			if (!Visible || !Ktisis.IsInGPose)
				return;

			var size = new Vector2(-1, -1);
			ImGui.SetNextWindowSize(size, ImGuiCond.FirstUseEver);
			ImGui.SetNextWindowSizeConstraints(new Vector2(ImGui.GetFontSize() * 16, 1), new Vector2(50000, 50000));
			ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(10, 10));

			if (ImGui.Begin("Advanced", ref Visible, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.AlwaysAutoResize)) {

				var target = Ktisis.GPoseTarget;
				var actor = (Actor*)target!.Address;

				if (actor->Model != null) {
					// Animation Controls
					AnimationControls.Draw();

					// Gaze Controls
					if (ImGui.CollapsingHeader("Gaze Control")) {
						if (PoseHooks.PosingEnabled)
							ImGui.TextWrapped("Gaze controls are unavailable while posing.");
						else
							EditGaze.Draw();
					}
					
					// Advanced
					if (ImGui.CollapsingHeader("Advanced (Debug)")) {
						if (ImGui.Button("Reset Current Pose") && actor->Model != null)
							actor->Model->SyncModelSpace();

						if (ImGui.Button("Set to Reference Pose") && actor->Model != null)
							actor->Model->SyncModelSpace(true);

						if (ImGui.Button("Store Pose") && actor->Model != null)
							Workspace.Workspace._TempPose.Store(actor->Model->Skeleton);
						ImGui.SameLine();
						if (ImGui.Button("Apply Pose") && actor->Model != null)
							Workspace.Workspace._TempPose.Apply(actor->Model->Skeleton);

						if (ImGui.Button("Force Redraw"))
							actor->Redraw();
					}
				}
			}

			ImGui.PopStyleVar();
			ImGui.End();
		}
	}

}