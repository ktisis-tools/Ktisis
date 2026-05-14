using System.Numerics;

using Dalamud.Interface;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Bindings.ImGui;

using Ktisis.Helpers;
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
		private static bool Dismissed = false;
		
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

				if (!Dismissed) {
					ImGui.Spacing();
					ImGui.Separator();
					ImGui.Spacing();
					ImGui.TextColored(ColYellow, "You're not using the latest Ktisis version!");
					ImGui.TextWrapped("In the coming months, Ktisis Alpha will be replaced with our v0.3 / Testing version, AKA Ktisis Workspace, which has been in development since 2024.");
					ImGui.TextWrapped("This Alpha version is missing a variety of stability & feature updates, and is a low priority for updates going forward until v0.3 is officially released.");
					ImGui.TextWrapped("We encourage current v0.2 users to try the Testing version and provide feedback in our Discord to help shape its development. "
						+ "Testing is available from the Dalamud Plugin Installer, and detailed instructions can be found in our #ktisis-faq channel. Thank you!");

					ImGui.Spacing();
					if (ImGui.Button("Dismiss"))
						Dismissed = true;
					ImGui.SameLine(0, ImGui.GetStyle().ItemInnerSpacing.X);
					if (ImGui.Button("Our Discord"))
						Common.OpenBrowser("https://discord.gg/kUG3W8B8Ny");
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
