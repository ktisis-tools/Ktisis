using System.Numerics;

using ImGuiNET;
using ImGuizmoNET;

using Dalamud.Logging;
using Dalamud.Interface;
using Dalamud.Interface.Components;

using Ktisis.Util;
using Ktisis.Interop;
using Ktisis.Overlay;
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

		public const ImGuiTreeNodeFlags BaseFlags = ImGuiTreeNodeFlags.OpenOnArrow;

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
				if (GuiHelpers.IconButtonTooltip(FontAwesomeIcon.PencilAlt, "Edit targeted Actor's appearance.")) {
					EditActor.Show();
				}

				// Config

				var cfg = Ktisis.Configuration;

				ImGui.SameLine();
				if (ImGuiComponents.IconButton(FontAwesomeIcon.Cog))
					ConfigGui.Show();

				Coordinates();

				ImGui.Separator();
				
				if (!Ktisis.IsInGPose && PoseHooks.PosingEnabled)
					PoseHooks.DisablePosing();

				ImGui.BeginDisabled(!Ktisis.IsInGPose);
				var pose = PoseHooks.PosingEnabled;
				if (ImGui.Checkbox("Toggle Posing", ref pose)) {
					PoseHooks.TogglePosing();
				}
				ImGui.EndDisabled();

				var showSkeleton = cfg.ShowSkeleton;
				if (ImGui.Checkbox("Toggle Skeleton", ref showSkeleton))
					Skeleton.Toggle();

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

				ImGui.Separator();

				// Bone tree

				DrawBoneTree();
			}

			ImGui.PopStyleVar(1);
			ImGui.End();
		}

		// Coordinates table

		private static unsafe bool Coordinates() {
			if (Ktisis.GPoseTarget == null) return false;

			ImGui.Separator();

			var target = (Actor*)Ktisis.GPoseTarget.Address;
			if (target->Model == null) return false;

			string targetName = !Ktisis.Configuration.DisplayCharName || target->Name == null ? "target" : target->Name!;
			string title = $"Transforming {targetName}";

			if (!Skeleton.HasSelectedBone) {
				ImGui.TextDisabled(title);
				var model = target->Model;
				return Transform.Draw(ref model->Position, ref model->Rotation, ref model->Scale);
			}

			var bone = Skeleton.SelectedBone;

			ImGui.TextDisabled($"{title}'s {Locale.GetBoneName(bone.HkaBone.Name.String)}");

			var trans = bone.Transform;
			if (Transform.Draw(Skeleton.UpdateSelect, ref trans.Translation, ref trans.Rotation, ref trans.Scale)) {
				var skele = target->Model->Skeleton->PartialSkeletons[bone._Partial];
				var pose = skele.GetHavokPose(0);
				pose->ModelPose[bone.Index] = trans;
				return true;
			}

			return false;
		}

		// Bone Tree

		public static void DrawBoneTree() {
			/*var editor = KtisisGui.SkeletonEditor;
			if (editor.Skeleton != null && editor.Skeleton.Count > 0)
				DrawBoneTree(editor.Skeleton[0].Bones[0]);

			GuiHelpers.DrawBoneNode("actor_target", ImGuiTreeNodeFlags.Leaf, "Actor", () => KtisisGui.SkeletonEditor.SelectActorTarget());*/
		}

		/*public static void DrawBoneTree(Bone bone) {
			var flag = BaseFlags;

			if (KtisisGui.SkeletonEditor.BoneSelector.IsSelected(bone))
				flag |= ImGuiTreeNodeFlags.Selected;

			var children = bone.GetChildren();
			if (children.Count == 0)
				flag |= ImGuiTreeNodeFlags.Leaf;

			var show = bone.IsRoot; 

			if (!show) show = GuiHelpers.DrawBoneNode(bone.HkaBone.Name, flag, Locale.GetBoneName(bone.HkaBone.Name!),() => KtisisGui.SkeletonEditor.SelectBone(bone));
			
			if (show) {
				// Show children
				foreach (var child in children)
					DrawBoneTree(child);
				ImGui.TreePop();
			}
		}*/
	}
}
