using System.Numerics;

using ImGuiNET;
using ImGuizmoNET;

using Dalamud.Interface;
using Dalamud.Interface.Components;

using Ktisis.Util;
using Ktisis.Overlay;
using Ktisis.Structs;
using Ktisis.Localization;
using Ktisis.Structs.Actor;
using Ktisis.Structs.Bones;
using Ktisis.Interop.Hooks;
using Ktisis.Interface.Components;
using Ktisis.Interface.Windows.ActorEdit;

namespace Ktisis.Interface.Windows {
	public static class Workspace {
		public static bool Visible = false;

		public static Vector4 ColGreen = new Vector4(0, 255, 0, 255);
		public static Vector4 ColRed = new Vector4(255, 0, 0, 255);

		public static TransformTable Transform = new();

		// Toggle visibility

		public static void Show() {
			Visible = true;
		}

		public static void Hide() {
			Visible = false;
		}

		// Draw window

		public static void Draw() {
			if (!Visible)
				return;

			var gposeOn = Ktisis.IsInGPose;

			var size = new Vector2(-1, -1);
			ImGui.SetNextWindowSize(size, ImGuiCond.Always);
			ImGui.SetNextWindowSizeConstraints(size, size);

			ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(10, 10));

			if (ImGui.Begin("Ktisis (Alpha)", ref Visible, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.AlwaysAutoResize)) {
				ImGui.BeginGroup();
				ImGui.AlignTextToFramePadding();

				ImGui.TextColored(
					gposeOn ? ColGreen : ColRed,
					gposeOn ? "GPose Enabled" : "GPose Disabled"
				);


				ImGui.SameLine();
				ImGui.SetCursorPosX(ImGui.CalcTextSize("GPose Disabled").X + (ImGui.GetFontSize() * 8)); // Prevents text overlap

				ImGui.BeginDisabled(!Ktisis.IsInGPose);
				var pose = PoseHooks.PosingEnabled;
				if(Ktisis.IsInGPose) ImGui.PushStyleColor(ImGuiCol.Text, pose ? ColGreen : ColRed);
				var label = pose ? "Posing" : "Not Posing";
				float toggleWidth = ImGui.GetFrameHeight() * 1.55f;
				float offsetWidth = GuiHelpers.GetRightOffset(toggleWidth);
				GuiHelpers.TextRight(label, offsetWidth);
				if (Ktisis.IsInGPose) ImGui.PopStyleColor();
				ImGui.SameLine();

				if (!Ktisis.IsInGPose)
					ImGuiComponents.DisabledToggleButton("Toggle Posing", false);
				else
					if (GuiHelpers.ToggleButton("Toggle Posing", ref pose, pose ? ColGreen : ColRed))
						PoseHooks.TogglePosing();

				if (!Ktisis.IsInGPose && PoseHooks.PosingEnabled)
					PoseHooks.DisablePosing();

				ImGui.EndDisabled();

				// control buttons (gizmo op + extra)
				ControlButtons.Draw();

				// Actor control

				ImGui.Separator();
				ActorControl();
			}

			ImGui.PopStyleVar(1);
			ImGui.End();
		}

		// Actor control

		private unsafe static void ActorControl() {
			var cfg = Ktisis.Configuration;

			// Get target actor, model, etc

			var target = Ktisis.GPoseTarget;
			if (target == null) return;

			var actor = (Actor*)Ktisis.GPoseTarget!.Address;
			if (actor->Model == null) return;

			// Draw co-ordinate table
			Coordinates(actor);

			// Animation control
			if (ImGui.CollapsingHeader("Animation Control")) {
				var control = PoseHooks.GetAnimationControl(target);
				if (PoseHooks.PosingEnabled || !Ktisis.IsInGPose || PoseHooks.IsGamePlaybackRunning(target) || control == null) {
					ImGui.Text("Animation Control is available when:");
					ImGui.BulletText("Game animation is paused");
					ImGui.BulletText("Posing is off");
				}
				else
					GuiHelpers.AnimationControls(control);
			}

			// Bone categories
			if (ImGui.CollapsingHeader("Toggle Bone Categories  ")) {

				ImGui.Indent(16.0f);
				foreach (Category category in Category.Categories.Values) {
					if (!category.ShouldDisplay) continue;

					bool categoryState = cfg.IsBoneCategoryVisible(category);
					if (!cfg.ShowSkeleton) categoryState = false;

					if (ImGui.Checkbox(category.Name, ref categoryState)) {
						if (!cfg.ShowSkeleton && categoryState) {
							cfg.ShowSkeleton = true;
						}
						cfg.ShowBoneByCategory[category.Name] = categoryState;
					}
				}
				ImGui.Unindent(16.0f);
			}

			// Bone tree
			BoneTree.Draw(actor);
		}

		// Coordinates table

		private static unsafe bool Coordinates(Actor* target) {
			string targetName = target->GetNameOr("target");
			string title = $"Transforming {targetName}";

			var select = Skeleton.BoneSelect;
			if (!select.Active) {
				ImGui.TextDisabled(title);
				var model = target->Model;
				return Transform.Draw(ref model->Position, ref model->Rotation, ref model->Scale);
			}

			var bone = Skeleton.GetSelectedBone(target->Model->Skeleton);
			if (bone == null) return false;

			ImGui.TextDisabled($"{title}'s {Locale.GetBoneName(bone.HkaBone.Name.String)}");

			return Transform.Draw(bone);
		}

		// Bone Tree


	}
}
