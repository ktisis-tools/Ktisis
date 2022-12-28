using ImGuiNET;

using Dalamud.Interface.Components;

using Ktisis.Interop.Hooks;
using Ktisis.Interface.Widgets;

namespace Ktisis.Interface.Components.Posing {
    internal class PoseState {
		// GPose Enabled / Anamnesis Enabled
		internal static void DrawPoseState(bool gposeOn = false) {
			ImGui.BeginGroup();

			ImGui.TextColored(
				gposeOn ? Colors.ColGreen : Colors.ColRed,
				gposeOn ? "GPose Enabled" : "GPose Disabled"
			);

			if (PoseHooks.AnamPosingEnabled)
				ImGui.TextColored(Colors.ColYellow, "Anamnesis Enabled");

			ImGui.EndGroup();
		}

		// Posing mode switch
		internal static void DrawPoseSwitch(bool gposeOn = false) {
			ImGui.BeginGroup();
			ImGui.BeginDisabled(!gposeOn);

			var enabled = PoseHooks.PosingEnabled;

			if (gposeOn) ImGui.PushStyleColor(ImGuiCol.Text, enabled ? Colors.ColGreen : Colors.ColRed);

			var width = ImGui.GetFrameHeight() * 1.55f;
			Text.LabelRight(enabled ? "Posing" : "Not Posing", Align.GetRightOffset(width));

			if (gposeOn) ImGui.PopStyleColor();

			ImGui.SameLine();
			if (gposeOn) {
				if (Buttons.ToggleButton("Toggle Posing", ref enabled, enabled ? Colors.ColGreen : Colors.ColRed))
					PoseHooks.TogglePosing();
			} else {
				ImGuiComponents.DisabledToggleButton("Toggle Posing", false);
			}

			ImGui.EndDisabled();
			ImGui.EndGroup();
		}
	}
}
