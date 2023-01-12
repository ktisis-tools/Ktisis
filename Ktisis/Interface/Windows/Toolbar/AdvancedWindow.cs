using System.Numerics;

using ImGuiNET;

using Ktisis.Interface.Components;
using Ktisis.Interface.Windows.Workspace;
using Ktisis.Interop.Hooks;
using Ktisis.Structs.Actor;

namespace Ktisis.Interface.Windows.Toolbar {
	public static class AdvancedWindow {
		private static bool Visible = false;

		/* FIXME: This seems unused? */
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
					AnimationControls.Draw(target);

					// Gaze Controls
					if (ImGui.CollapsingHeader("Gaze Control")) {
						if (PoseHooks.PosingEnabled)
							ImGui.TextWrapped("Gaze controls are unavailable while posing.");
						else
							EditGaze.Draw(actor);
					}
					
					// Status Effect control
					StatusEffectControls.Draw(actor);
					
					// Advanced
					if (ImGui.CollapsingHeader("Advanced (Debug)")) {
						Workspace.Workspace.DrawAdvancedDebugOptions(actor);
					}
				}
			}

			ImGui.PopStyleVar();
			ImGui.End();
		}

	}

}
