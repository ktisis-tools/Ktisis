using System;
using System.Numerics;

using ImGuiNET;

using Dalamud.Logging;

using FFXIVClientStructs.Havok;

using Ktisis.Structs;
using Ktisis.Structs.Actor;
using Ktisis.Structs.Bones;
using Ktisis.Localization;

namespace Ktisis.Overlay {
	public static class Skeleton {
		// because C# hates nullable structs for some reason.
		public static bool UpdateSelect = false;
		public static bool HasSelectedBone = false;
		public static Bone SelectedBone;

		public static void Toggle() {
			var visible = !Ktisis.Configuration.ShowSkeleton;
			Ktisis.Configuration.ShowSkeleton = visible;
			if (!visible && HasSelectedBone) {
				HasSelectedBone = false;
				OverlayWindow.SetGizmoOwner(null);
			}
		}

		public unsafe static void Draw() {
			UpdateSelect = false;
			HasSelectedBone = false;

			// Fetch actor, model & skeleton

			if (Ktisis.GPoseTarget == null) return;

			var actor = (Actor*)Ktisis.GPoseTarget!.Address;
			var model = actor->Model;
			if (model == null) return;

			// ImGui rendering

			var draw = ImGui.GetWindowDrawList();

			// Draw skeleton

			var skele = model->Skeleton;

			// Iterate partial skeletons
			var sync = false;
			for (var p = 0; p < skele->PartialSkeletonCount; p++) {
				var partial = skele->PartialSkeletons[p];
				var pose = partial.GetHavokPose(0);
				if (pose == null) continue;

				// Iterate bones
				var bones = pose->GetBones();
				foreach (Bone bone in bones) {
					if (!Ktisis.Configuration.IsBoneVisible(bone) || bone.Index == 0)
						continue; // Bone is hidden, move onto the next one.

					var boneName = bone.HkaBone.Name.String;
					var gizmoId = $"{p}_{boneName}";

					// Fetch bone category color & convert world pos to screen

					var boneColor = ImGui.GetColorU32(Ktisis.Configuration.GetCategoryColor(bone));
					Dalamud.GameGui.WorldToScreen(bone.GetWorldPos(model), out var pos2d);

					// Draw line to bone parent if any

					if (bone.ParentIndex > 0) {
						var parent = bones[bone.ParentIndex];

						Dalamud.GameGui.WorldToScreen(parent.GetWorldPos(model), out var posParent);

						var lineThickness = Math.Max(0.01f, Ktisis.Configuration.SkeletonLineThickness / Dalamud.Camera->Camera->InterpDistance * 2f);
						draw.AddLine(pos2d, posParent, boneColor, lineThickness);
					}

					// Add selectable item

					if (bone.HkaBone.Name.String != "j_ago" || p == 0) {
						var item = Selection.AddItem($"{Locale.GetBoneName(boneName)}##{p}", pos2d, boneColor);
						if (item.IsClicked()) {
							UpdateSelect = true;
							OverlayWindow.SetGizmoOwner(gizmoId);
						}
					}

					// Bone selection & gizmo

					var gizmo = OverlayWindow.GetGizmo(gizmoId);
					if (gizmo != null) {
						var matrix = gizmo.Matrix;
						bone.Transform.get4x4ColumnMajor(&matrix.M11);

						matrix.Translation *= model->Height;
						gizmo.Matrix = Matrix4x4.Transform(matrix, model->Rotation);
						gizmo.Matrix.Translation += model->Position;

						if (gizmo.Draw()) {
							gizmo.Matrix.Translation -= model->Position;
							matrix = Matrix4x4.Transform(gizmo.Matrix, Quaternion.Inverse(model->Rotation));
							matrix.Translation /= model->Height;

							pose->AccessBoneModelSpace(bone.Index, hkaPose.PropagateOrNot.Propagate)->set((hkMatrix4f*)&matrix);

							sync = true;
							UpdateSelect = true;
						}

						HasSelectedBone = true;
						SelectedBone = bone;
						SelectedBone._Partial = p;
					} else if (HasSelectedBone && SelectedBone.HkaBone.Name.String == bone.HkaBone.Name.String) {
						// this is jank as fuck. as far as I'm aware this only exists for the jaw bone?
						*pose->AccessBoneModelSpace(bone.Index, hkaPose.PropagateOrNot.Propagate) = SelectedBone.Transform;
					}
				}

				if (sync)
					Interop.PoseHooks.SyncModelSpaceHook.Original(pose);
			}
		}
	}
}