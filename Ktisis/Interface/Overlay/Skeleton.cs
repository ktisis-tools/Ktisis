using System;
using System.Numerics;

using ImGuiNET;

using ImGuizmoNET;

using Ktisis.Posing;
using Ktisis.Services;
using Ktisis.Structs.Actor;
using Ktisis.Library.Extensions;

namespace Ktisis.Interface.Overlay {
	public static class Skeleton {
		// Allow other classes to retrieve the currently selected bone.
		// OverlayWindow sets Active and Update to false before every Draw call.
		// Changing these values will not change the selection, that's based on the Gizmo's owner ID.
		public static (
			bool Active, // A bone is currently selected.
			bool Update, // Signal to any class that caches transforms, that they need to be updated.
			int Partial,
			int Index,
			string Name
		) BoneSelect;

		public static bool IsBoneSelected(Bone bone) => BoneSelect.Active && BoneSelect.Partial == bone.Partial && BoneSelect.Index == bone.Index;

		public static void Toggle() {
			var visible = !Ktisis.Configuration.ShowSkeleton;
			Ktisis.Configuration.ShowSkeleton = visible;
		}

		public unsafe static void Draw() {
			// Fetch actor, model & skeleton

			if (GPoseService.GPoseTarget == null) return;
			var actor = (Actor*)GPoseService.GPoseTarget!.Address;
			var model = actor->Model;
			if (model == null) return;

			var world = OverlayWindow.WorldMatrix;

			// ImGui rendering

			var draw = ImGui.GetWindowDrawList();

			// Make selectable model root

			{
				var actorName = actor->GetNameOr("Actor");
				var gizmo = OverlayWindow.GetGizmo(actorName);
				if (gizmo != null) {
					var matrix = InteropService.GetMatrix(&model->Transform);
					gizmo.Matrix = matrix;
					if (gizmo.Draw())
					{
						matrix = gizmo.Matrix;
						InteropService.SetMatrix(&model->Transform, matrix);
					}
				} else {
					world->WorldToScreen(model->Position, out var pos2d);
					if (Selection.AddItem(actorName, pos2d).IsClicked())
						OverlayWindow.SetGizmoOwner(actorName);
				}
			}

			// Draw skeleton

			var isUsing = ImGuizmo.IsUsing();

			for (var p = 0; p < model->Skeleton->PartialSkeletonCount; p++) {
				var partial = model->Skeleton->PartialSkeletons[p];
				var pose = partial.GetHavokPose(0);
				if (pose == null) continue;

				var skeleton = pose->Skeleton;
				for (var i = 1; i < skeleton->Bones.Length; i++) {
					var bone = model->Skeleton->GetBone(p, i);
					var boneName = bone.HkaBone.Name.String;
					var parentId = bone.ParentId;

					var uniqueName = bone.UniqueName;

					if (!Ktisis.Configuration.IsBoneVisible(bone))
						continue; // Bone is hidden, move onto the next one.

					// Access bone transform
					var transform = bone.AccessModelSpace();

					if (bone.IsBusted())
						continue; // bone's busted, skip it.

					// Get bone color and screen position
					var boneColRgb = Ktisis.Configuration.GetCategoryColor(bone);
					if (isUsing)
						boneColRgb.W *= Ktisis.Configuration.SkeletonLineOpacityWhileUsing;
					else
						boneColRgb.W *= Ktisis.Configuration.SkeletonLineOpacity;

					var boneColor = ImGui.GetColorU32(boneColRgb);
					var isVisible = world->WorldToScreen(bone.GetWorldPos(model), out var pos2d);

					// Draw line to bone parent if any
					if (parentId > 0 && Ktisis.Configuration.DrawLinesOnSkeleton && !(!Ktisis.Configuration.DrawLinesWithGizmo && OverlayWindow.GizmoOwner != null)) {
						// TODO: Draw lines for parents of partials.

						var parent = model->Skeleton->GetBone(p, parentId);
						if (Ktisis.Configuration.IsBoneVisible(parent)) {
							var lineThickness = Math.Max(0.01f, Ktisis.Configuration.SkeletonLineThickness / DalamudServices.Camera->Camera->InterpDistance * 2f);
							isVisible &= world->WorldToScreen(parent.GetWorldPos(model), out var parentPos2d);
							if (isVisible)
								draw.AddLine(pos2d, parentPos2d, boneColor, lineThickness);
						}
					}

					// Create selectable item
					// TODO: Hide when moving gizmo?
					if (!IsBoneSelected(bone) && !(boneName == "j_ago" && p == 0)) {
						var item = Selection.AddItem(uniqueName, pos2d, boneColor);
						if (item.IsClicked()) {
							BoneSelect.Update = true;
							BoneSelect.Name = boneName ?? "Unknown";
							OverlayWindow.SetGizmoOwner(uniqueName);
						}
					}

					// Bone selection & gizmo
					var gizmo = OverlayWindow.GetGizmo(uniqueName);
					if (gizmo != null) {
						var matrix = InteropService.GetMatrix(transform);

						// Apply the root transform of the actor's model.
						// This is important for the gizmo's orientation to show correctly.
						matrix.Translation *= model->Height * model->Scale;
						gizmo.Matrix = Matrix4x4.Transform(matrix, model->Rotation);
						gizmo.Matrix.Translation += model->Position;

						// Draw the gizmo. This returns true if it has been moved.
						if (gizmo.Draw() || gizmo.ManipulateEuler()) {
							BoneSelect.Update = true;

							// Reverse the previous transform we did.
							gizmo.Matrix.Translation -= model->Position;
							matrix = Matrix4x4.Transform(gizmo.Matrix, Quaternion.Inverse(model->Rotation));
							matrix.Translation /= model->Height * model->Scale;

							// Write our updated matrix to memory.
							var initialRot = transform->Rotation.ToQuat();
							var initialPos = transform->Translation.ToVector3();
							InteropService.SetMatrix(transform, matrix);

							// handles parenting

							if (Ktisis.Configuration.EnableParenting)
								bone.PropagateChildren(transform, initialPos, initialRot);

							// handles linking
							if (boneName != null && (boneName.EndsWith("_l") || boneName.EndsWith("_r"))) {
								var siblingBone = bone.GetMirrorSibling();
								if (siblingBone != null)
									siblingBone.PropagateSibling(transform->Rotation.ToQuat() / initialRot, Ktisis.Configuration.SiblingLink);
							}
						}

						BoneSelect.Active = true;
						BoneSelect.Partial = p;
						BoneSelect.Index = i;
					}
				}
			}
		}

		public unsafe static Bone? GetSelectedBone() {
			if (!BoneSelect.Active) return null;

			var target = GPoseService.GPoseTarget;
			if (target == null) return null;

			var model = ((Actor*)target.Address)->Model;
			if (model == null) return null;

			return model->Skeleton->GetBone(BoneSelect.Partial, BoneSelect.Index);
		}
	}
}