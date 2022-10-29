using System.Numerics;

using ImGuiNET;
using ImGuizmoNET;

using Dalamud.Interface;
using Dalamud.Interface.Components;

using Ktisis.Util;
using Ktisis.Interop;
using Ktisis.Overlay;
using Ktisis.Structs;
using Ktisis.Localization;
using Ktisis.Structs.Bones;
using Ktisis.Structs.Actor;
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

				// Gizmo Controls
				// TODO

				if (ImGuiComponents.IconButton(FontAwesomeIcon.LocationArrow))
					Ktisis.Configuration.GizmoOp = OPERATION.TRANSLATE;

				ImGui.SameLine();
				if (ImGuiComponents.IconButton(FontAwesomeIcon.Sync))
					Ktisis.Configuration.GizmoOp = OPERATION.ROTATE;

				ImGui.SameLine();
				if (ImGuiComponents.IconButton(FontAwesomeIcon.ExpandAlt))
					Ktisis.Configuration.GizmoOp = OPERATION.SCALE;

				ImGui.SameLine();
				if (ImGuiComponents.IconButton(FontAwesomeIcon.DotCircle))
					Ktisis.Configuration.GizmoOp = OPERATION.UNIVERSAL;

				// Second row

				var gizmode = Ktisis.Configuration.GizmoMode;
				if (GuiHelpers.IconButtonTooltip(gizmode == MODE.WORLD ? FontAwesomeIcon.Globe : FontAwesomeIcon.Home, "Local / World orientation mode switch."))
					Ktisis.Configuration.GizmoMode = gizmode == MODE.WORLD ? MODE.LOCAL : MODE.WORLD;

				ImGui.SameLine();
				if (GuiHelpers.IconButtonTooltip(FontAwesomeIcon.PencilAlt, "Edit targeted Actor's appearance."))
					EditActor.Show();

				// Config

				var cfg = Ktisis.Configuration;

				ImGui.SameLine();
				if (ImGuiComponents.IconButton(FontAwesomeIcon.Cog))
					ConfigGui.Show();

				ImGui.Separator();
				
				if (!Ktisis.IsInGPose && PoseHooks.PosingEnabled)
					PoseHooks.DisablePosing();

				ImGui.BeginDisabled(!Ktisis.IsInGPose);
				var pose = PoseHooks.PosingEnabled;
				if (ImGui.Checkbox("Toggle Posing", ref pose))
					PoseHooks.TogglePosing();
				ImGui.EndDisabled();

				var showSkeleton = cfg.ShowSkeleton;
				if (ImGui.Checkbox("Toggle Skeleton", ref showSkeleton))
					Skeleton.Toggle();

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
				if (!Ktisis.IsInGPose || PoseHooks.IsGamePlaybackRunning(target) || control == null)
					ImGui.Text("Unavailable at this time.");
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
			if (ImGui.CollapsingHeader("Bone List")) {
				if (ImGui.BeginListBox("##bone_list", new Vector2(-1, 300))) {
					var body = actor->Model->Skeleton->GetBone(0, 1);
					DrawBoneTreeNode(body);
					ImGui.EndListBox();
				}
			}
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

			var trans = bone.Transform;
			if (Transform.Draw(select.Update, ref trans.Translation, ref trans.Rotation, ref trans.Scale)) {
				bone.Transform = trans;
				return true;
			}

			return false;
		}

		// Bone Tree

		public unsafe static void DrawBoneTreeNode(Bone bone) {
			var children = bone.GetChildren();

			var flag = children.Count > 0 ? ImGuiTreeNodeFlags.OpenOnArrow : ImGuiTreeNodeFlags.Leaf;
			if (Skeleton.IsBoneSelected(bone))
				flag |= ImGuiTreeNodeFlags.Selected;

			var show = GuiHelpers.DrawBoneNode(bone, flag, () => OverlayWindow.SetGizmoOwner(bone.UniqueName));
			if (show) {
				foreach (var child in children)
					DrawBoneTreeNode(child);
				ImGui.TreePop();
			}
		}
	}
}
