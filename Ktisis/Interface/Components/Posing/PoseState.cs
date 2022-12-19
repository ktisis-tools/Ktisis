using ImGuiNET;

using Dalamud.Interface.Components;

using Ktisis.Interop.Hooks;
using Ktisis.Interface.Library;

namespace Ktisis.Interface.Components.Posing {
    internal class PoseState {
		// GPose Enabled / Anamnesis Enabled
		internal static void DrawPoseState(bool gposeOn = false) {
			ImGui.BeginGroup();

			ImGui.TextColored(
				gposeOn ? Common.ColGreen : Common.ColRed,
				gposeOn ? "GPose Enabled" : "GPose Disabled"
			);

			if (PoseHooks.AnamPosingEnabled)
				ImGui.TextColored(Common.ColYellow, "Anamnesis Enabled");

			ImGui.EndGroup();
		}

		// Posing mode switch
		internal static void DrawPoseSwitch(bool gposeOn = false) {
			ImGui.BeginGroup();
			ImGui.BeginDisabled(!gposeOn);

			var enabled = PoseHooks.PosingEnabled;

			if (gposeOn) ImGui.PushStyleColor(ImGuiCol.Text, enabled ? Common.ColGreen : Common.ColRed);

			var width = ImGui.GetFrameHeight() * 1.55f;
			Text.LabelRight(enabled ? "Posing" : "Not Posing", Align.GetRightOffset(width));

			if (gposeOn) ImGui.PopStyleColor();

			ImGui.SameLine();
			if (gposeOn) {
				if (Buttons.ToggleButton("Toggle Posing", ref enabled, enabled ? Common.ColGreen : Common.ColRed))
					PoseHooks.TogglePosing();
			} else {
				ImGuiComponents.DisabledToggleButton("Toggle Posing", false);
			}

			ImGui.EndDisabled();
			ImGui.EndGroup();
		}
	}
}
