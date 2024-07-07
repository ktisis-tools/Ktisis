using System.Numerics;

using ImGuiNET;

using Dalamud.Interface;
using Dalamud.Game.ClientState.Objects.Types;

using Ktisis.Util;
using Ktisis.Overlay;
using Ktisis.Structs.Actor;
using Ktisis.Localization;
using Ktisis.Interop.Hooks;
using Ktisis.Interface.Components;
using Ktisis.Interface.Windows.Workspace.Tabs;

namespace Ktisis.Interface.Windows.Workspace {
    public static class Workspace {
		public static bool Visible = false;
		
		public static Vector4 ColGreen = new Vector4(0, 255, 0, 255);
		public static Vector4 ColYellow = new Vector4(255, 250, 0, 255);
		public static Vector4 ColRed = new Vector4(255, 0, 0, 255);

		// Toggle visibility

		public static void Show() => Visible = true;
		public static void Toggle() => Visible = !Visible;
		
		public static void OnEnterGposeToggle(bool isInGpose) {
			if (Ktisis.Configuration.OpenKtisisMethod == OpenKtisisMethod.OnEnterGpose)
				Visible = isInGpose;
		}

		public static float PanelHeight => ImGui.GetTextLineHeight() * 2 + ImGui.GetStyle().ItemSpacing.Y + ImGui.GetStyle().FramePadding.Y;

		// Draw window

		public static void Draw() {
			if (!Visible)
				return;

			var gposeOn = Ktisis.IsInGPose;

			var size = new Vector2(-1, -1);
			ImGui.SetNextWindowSize(size, ImGuiCond.FirstUseEver);

			ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(10, 10));

			var displaySize = ImGui.GetIO().DisplaySize;
			var maxSize = new Vector2(displaySize.X / 4f, displaySize.Y * 0.9f);
			ImGui.SetNextWindowSizeConstraints(Vector2.Zero, maxSize);
			if (ImGui.Begin($"Ktisis ({Ktisis.Version})", ref Visible, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.AlwaysAutoResize)) {

				ControlButtons.PlaceAndRenderSettings();

				ImGui.BeginGroup();
				ImGui.AlignTextToFramePadding();

				ImGui.TextColored(
					gposeOn ? ColGreen : ColRed,
					gposeOn ? "GPose Enabled" : "GPose Disabled"
				);

				if (PoseHooks.AnamPosingEnabled) {
					ImGui.TextColored(
						ColYellow,
						"Anamnesis Enabled"	
					);
				}

				ImGui.EndGroup();

				ImGui.SameLine();

				// Pose switch
				ControlButtons.DrawPoseSwitch();

				var target = Ktisis.GPoseTarget;
				if (target == null) return;

				// Selection info
				ImGui.Spacing();
				SelectInfo(target);

				// Actor control

				ImGui.Spacing();
				ImGui.Separator();

				if (ImGui.BeginTabBar(Locale.GetString("Workspace"))) {
					if (ImGui.BeginTabItem(Locale.GetString("Actor")))
						ActorTab.Draw(target);
					if (ImGui.BeginTabItem(Locale.GetString("Pose")))
						PoseTab.Draw(target);
					if (ImGui.BeginTabItem(Locale.GetString("Camera")))
						CameraTab.Draw();
					if (ImGui.BeginTabItem("World"))
						WorldTab.Draw();
				}
			}

			ImGui.PopStyleVar();
			ImGui.End();
		}
		
		// Selection details

		private unsafe static void SelectInfo(IGameObject target) {
			var actor = (Actor*)target.Address;

			var select = Skeleton.BoneSelect;
			var bone = Skeleton.GetSelectedBone();

			var frameSize = new Vector2(ImGui.GetContentRegionAvail().X - GuiHelpers.WidthMargin(), PanelHeight);
			ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(ImGui.GetStyle().FramePadding.X, ImGui.GetStyle().FramePadding.Y / 2));
			if (ImGui.BeginChildFrame(8, frameSize, ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoScrollbar)) {
				GameAnimationIndicator();

				ImGui.BeginGroup();

				// display target name
				ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (ImGui.GetStyle().FramePadding.Y / 2));
				ImGui.Text(actor->GetNameOrId());

				// display selected bone name
				ImGui.SetCursorPosY(ImGui.GetCursorPosY() - (ImGui.GetStyle().ItemSpacing.Y / 2) - (ImGui.GetStyle().FramePadding.Y / 2));
				if (select.Active && bone != null) {
					ImGui.Text($"{bone.LocaleName}");
				} else {
					ImGui.BeginDisabled();
					ImGui.Text("No bone selected");
					ImGui.EndDisabled();
				}

				ImGui.EndGroup();

				ImGui.EndChildFrame();
			}
			ImGui.PopStyleVar();
		}

		private static void GameAnimationIndicator() {
			var target = Ktisis.GPoseTarget;
			if (target == null) return;

			var isGamePlaybackRunning = PoseHooks.IsGamePlaybackRunning(target);
			var icon = isGamePlaybackRunning ? FontAwesomeIcon.Play : FontAwesomeIcon.Pause;

			var size = GuiHelpers.CalcIconSize(icon).X;

			ImGui.SameLine(size / 1.5f);

			ImGui.BeginGroup();

			ImGui.Dummy(new Vector2(size, size) / 2);

			GuiHelpers.Icon(icon);
			GuiHelpers.Tooltip(isGamePlaybackRunning ? "Game Animation is playing for this target." + (PoseHooks.PosingEnabled ? "\nPosing may reset periodically." : "") : "Game Animation is paused for this target." + (!PoseHooks.PosingEnabled ? "\nAnimation Control Can be used." : ""));

			ImGui.EndGroup();

			ImGui.SameLine(size * 2.5f);
		}
	}
}
