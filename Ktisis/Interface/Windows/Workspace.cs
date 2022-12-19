using System;
using System.Numerics;
using System.Collections.Generic;

using ImGuiNET;

using Dalamud.Interface;
using Dalamud.Game.ClientState.Objects.Types;

using Ktisis.Interop.Hooks;
using Ktisis.Structs.Actor;
using Ktisis.Interface.Editor;
using Ktisis.Interface.Library;
using Ktisis.Interface.Overlay;
using Ktisis.Interface.Components.Posing;

namespace Ktisis.Interface.Windows
{
    public static class Workspace {
		public static bool Visible = false;
		public static void Show() => Visible = true;
		public static void Toggle() => Visible = !Visible;

		public static List<Manipulable> Items = new();

		internal unsafe static void OnGPoseChange(bool state) {
			if (state) {
				var tar = Ktisis.GPoseTarget;
				if (tar != null) {
					var actor = (Actor*)tar.Address;
					Items.Add(new ActorObject(actor->ObjectID));
				}
			} else {
				Items.Clear();
			}

			if (Ktisis.Configuration.OpenKtisisMethod == OpenKtisisMethod.OnEnterGpose)
				Visible = state;
		}

		// Draw main window

		public static void Draw() {
			if (!Visible) return;

			var displaySize = ImGui.GetIO().DisplaySize;

			ImGui.SetNextWindowSizeConstraints(
				new Vector2(250, 124),
				new Vector2(displaySize.X * 0.25f, displaySize.Y * 0.75f)
			);

			if (ImGui.Begin($"Ktisis ({Ktisis.Version})", ref Visible)) {
				var gposeOn = Ktisis.IsInGPose;

				PoseState.DrawPoseState(gposeOn);
				ImGui.SameLine();
				PoseState.DrawPoseSwitch(gposeOn);

				ImGui.Spacing();

				DrawSelectState();

				ImGui.Spacing();
				//ImGui.Separator();

				DrawSceneTree();

				ImGui.End();
			}
		}

		// Draw selection state

		private unsafe static void DrawSelectState() {
			var gameObj = Ktisis.GPoseTarget;
			if (gameObj == null || gameObj.Address == IntPtr.Zero) return;

			var frameSize = new Vector2(
				ImGui.GetContentRegionAvail().X - Align.WidthMargin,
				ImGui.GetTextLineHeight() * 2 + ImGui.GetStyle().ItemSpacing.Y + ImGui.GetStyle().FramePadding.Y
			);
			ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(
				ImGui.GetStyle().FramePadding.X,
				ImGui.GetStyle().FramePadding.Y / 2
			));

			if (ImGui.BeginChildFrame(8, frameSize, ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoScrollbar)) {
				var actor = (Actor*)gameObj.Address;
				var bone = Skeleton.GetSelectedBone();
				var select = Skeleton.BoneSelect;

				AnimStateIndicator(gameObj);

				ImGui.BeginGroup();

				var spacing = ImGui.GetStyle().ItemSpacing.Y / 2;
				var padding = ImGui.GetStyle().FramePadding.Y / 2;

				// display target name
				ImGui.SetCursorPosY(ImGui.GetCursorPosY() + padding);
				ImGui.Text(actor->GetNameOrId());

				// display selected bone name
				ImGui.SetCursorPosY(ImGui.GetCursorPosY() - spacing - padding);
				if (select.Active && bone != null)
					ImGui.Text(bone.LocaleName);
				else
					ImGui.TextDisabled("No bone selected");

				ImGui.EndGroup();
				ImGui.EndChildFrame();
			}
			ImGui.PopStyleVar();
		}

		private unsafe static void AnimStateIndicator(GameObject target) {
			var active = PoseHooks.IsGamePlaybackRunning(target);

			var icon = active ? FontAwesomeIcon.Play : FontAwesomeIcon.Pause;
			var size = Icons.CalcIconSize(icon).X;

			ImGui.SameLine(size / 1.5f);

			ImGui.BeginGroup();

			ImGui.Dummy(new Vector2(size, size) / 2);

			Icons.DrawIcon(icon);
			Common.Tooltip(active ? "Animation playback is active for this target." : "Animation playback is paused for this target.");

			ImGui.EndGroup();

			ImGui.SameLine(size * 2.5f);
		}

		// Draw scene tree

		private static void DrawSceneTree() {
			if (ImGui.BeginChildFrame(471, new Vector2(-1, -1), ImGuiWindowFlags.HorizontalScrollbar)) {
				foreach (var item in Items) {
					item.DrawTreeNode();
				}
			}
		}

		private static void DrawTreeItem() {

		}
	}
}
