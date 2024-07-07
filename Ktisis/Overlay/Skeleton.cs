using System;
using System.Collections.Generic;
using System.Numerics;

using ImGuiNET;
using ImGuizmoNET;

using Ktisis.Structs;
using Ktisis.Structs.Actor;
using Ktisis.Structs.Bones;
using Ktisis.Structs.Extensions;
using Ktisis.Interface.Windows.Toolbar;
using Ktisis.Interface.Windows.Workspace.Tabs;

namespace Ktisis.Overlay {
	public static class Skeleton {
		// Allow other classes to retrieve the currently selected bone.
		// OverlayWindow sets Active and Update to false before every Draw call.
		// Changing these values will not change the selection, that's based on the Gizmo's owner ID.
		public static (
			bool Active, // A bone is currently selected.
			bool Update, // Signal to any class that caches transforms, that they need to be updated.
			nint Child, // Address of child model if the selected bone belongs to it. Spaghetti.
			int Partial,
			int Index,
			string Name
		) BoneSelect;

		public static bool IsBoneSelected(Bone bone) {
			if (!BoneSelect.Active)
				return false;
			if (BoneSelect.Child != 0 && BoneSelect.Child != bone.PoseAddress)
				return false;
			return BoneSelect.Partial == bone.Partial && BoneSelect.Index == bone.Index;
		}

		public static void Toggle() {
			var visible = !Ktisis.Configuration.ShowSkeleton;
			Ktisis.Configuration.ShowSkeleton = visible;
		}

		public unsafe static void Draw() {
			// Fetch actor, model & skeleton

			var actor = Ktisis.Target;
			if (actor == null) return;
			
			var model = actor->Model;
			if (model == null || model->Skeleton == null) return;
			
			// Draw actor root
			DrawActorRoot(actor);

			// Draw model skeleton
			DrawModelSkeleton(model);
			
			// Draw children (weapons, props)
			// This iterates a linked list of child objects.

			var wepCategory = Category.GetByName("weapons");
			if (wepCategory != null && Ktisis.Configuration.IsBoneCategoryVisible(wepCategory)) {
				var setCategory = new List<Category> {wepCategory};
				var children = model->GetChildren();
				foreach (var ptr in children) {
					var child = (ActorModel*)ptr;
					if ((child->Flags & 9) == 0) continue;
					DrawModelSkeleton(child, model, setCategory);
				}
			}
		}
		
		public unsafe static void DrawModelSkeleton(ActorModel* model, ActorModel* parentModel = null, List<Category>? _setCategory = null) {
			if (model->Skeleton == null) return;
			
			// Fetch actor, model & skeleton

			var world = OverlayWindow.WorldMatrix;
			
			// Get model attachment
					
			var attach = model->Attach;
			var hasAttach = attach.Count == 1 && attach.Type == 4;

			// ImGui rendering

			var draw = ImGui.GetWindowDrawList();

			// Draw skeleton

			var isUsing = ImGuizmo.IsUsing() || PoseTab.Transform.IsActive || TransformWindow.Transform.IsActive;

			for (var p = 0; p < model->Skeleton->PartialSkeletonCount; p++) {
				var partial = model->Skeleton->PartialSkeletons[p];
				var pose = partial.GetHavokPose(0);
				if (pose == null) continue;
				
				var camera = Services.Camera->GetActiveCamera();

				var skeleton = pose->Skeleton;
				if (skeleton == null) continue;
				
				for (var i = 1; i < skeleton->Bones.Length; i++) {
					var bone = model->Skeleton->GetBone(p, i, parentModel != null);
					if (_setCategory != null) bone._setCategory = _setCategory;

					if (!Ktisis.Configuration.IsBoneVisible(bone))
						continue; // Bone is hidden, move onto the next one.

					// Access bone transform
					var transform = bone.AccessModelSpace();

					if (bone.IsBusted())
						continue; // bone's busted, skip it.
					
					var parentId = bone.ParentId;
					var boneName = bone.HkaBone.Name.String ?? "";
					var uniqueName = bone.UniqueName;

					// Get bone color and screen position
					var boneColRgb = Ktisis.Configuration.GetCategoryColor(bone);
					if (isUsing) boneColRgb.W *= Ktisis.Configuration.SkeletonLineOpacityWhileUsing;
					else boneColRgb.W *= Ktisis.Configuration.SkeletonLineOpacity;

					var worldPos = bone.GetWorldPos(model, parentModel);
					var boneColor = ImGui.GetColorU32(boneColRgb);
					var isVisible = world->WorldToScreenDepth(worldPos, out var pos2d);

					// Draw line to bone parent if any
					var minParent = hasAttach ? 1 : 0;
					if (parentId > minParent && Ktisis.Configuration.DrawLinesOnSkeleton && !(!Ktisis.Configuration.DrawLinesWithGizmo && OverlayWindow.GizmoOwner != null)) {
						// TODO: Draw lines for parents of partials.

						var parent = model->Skeleton->GetBone(p, parentId);
						if (_setCategory != null) parent._setCategory = _setCategory;
						if (Ktisis.Configuration.IsBoneVisible(parent)) {
							var pWorldPos = parent.GetWorldPos(model, parentModel);
							var dist = (
								camera->DistanceFrom(worldPos)
								+ camera->DistanceFrom(pWorldPos)
							) / 2;
							var lineThickness = Math.Max(0.01f, Ktisis.Configuration.SkeletonLineThickness / dist * 2f);
							isVisible &= world->WorldToScreen(pWorldPos, out var parentPos2d);
							if (isVisible)
								draw.AddLine(new Vector2(pos2d.X, pos2d.Y), parentPos2d, boneColor, lineThickness);
						}
					}

					// Create selectable item
					// TODO: Hide when moving gizmo?
					if (isVisible && !IsBoneSelected(bone) && !(boneName == "j_ago" && p == 0)) {
						var item = Selection.AddItem(uniqueName, pos2d, boneColor, bone.Index);
						if (item.IsClicked()) {
							BoneSelect.Update = true;
							BoneSelect.Name = boneName;
							OverlayWindow.SetGizmoOwner(uniqueName);
						}
					}

					// Bone selection & gizmo
					var gizmo = OverlayWindow.GetGizmo(uniqueName);
					if (gizmo != null) {
						var matrix = Interop.Alloc.GetMatrix(transform);

						var scale = model->Scale * model->Height;
						if (parentModel != null)
							scale *= parentModel->Height;
						if (hasAttach && attach.BoneAttach != null)
							scale *= model->GetAttachScale();

						// Apply the root transform of the actor's model.
						// This is important for the gizmo's orientation to show correctly.
						matrix.Translation *= scale;
						gizmo.Matrix = Matrix4x4.Transform(matrix, model->Rotation);
						gizmo.Matrix.Translation += model->Position;

						// Draw the gizmo. This returns true if it has been moved.
						if (gizmo.Draw() || gizmo.ManipulateEuler()) {
							BoneSelect.Update = true;

							// Reverse the previous transform we did.
							gizmo.Matrix.Translation -= model->Position;
							matrix = Matrix4x4.Transform(gizmo.Matrix, Quaternion.Inverse(model->Rotation));
							matrix.Translation /= scale;

							// Write our updated matrix to memory.
							var initialRot = transform->Rotation.ToQuat();
							var initialPos = transform->Translation.ToVector3();
							Interop.Alloc.SetMatrix(transform, matrix);

							// handles parenting

							if (Ktisis.Configuration.EnableParenting)
								bone.PropagateChildren(transform, initialPos, initialRot);

							// handles linking
							if (boneName.EndsWith("_l") || boneName.EndsWith("_r")) {
								var siblingBone = bone.GetMirrorSibling();
								if (siblingBone != null)
									siblingBone.PropagateSibling(transform->Rotation.ToQuat() / initialRot, Ktisis.Configuration.SiblingLink);
							}
						}

						BoneSelect.Active = true;
						BoneSelect.Partial = p;
						BoneSelect.Index = i;
						BoneSelect.Child = parentModel != null ? (nint)model : 0;
					}
				}
			}
		}

		public unsafe static void DrawActorRoot(Actor* actor) {
			var world = OverlayWindow.WorldMatrix;
			var model = actor->Model;
			
			var actorName = actor->GetNameOr("Actor");
			var gizmo = OverlayWindow.GetGizmo(actorName);
			if (gizmo != null) {
				var matrix = Interop.Alloc.GetMatrix(&model->Transform);
				gizmo.Matrix = matrix;
				if (gizmo.Draw()) {
					matrix = gizmo.Matrix;
					Interop.Alloc.SetMatrix(&model->Transform, matrix);
				}
			} else {
				var isVisible = world->WorldToScreenDepth(model->Position, out var pos2d);
				if (isVisible && Selection.AddItem(actorName, pos2d).IsClicked())
					OverlayWindow.SetGizmoOwner(actorName);
			}
		}

		public unsafe static Bone? GetSelectedBone() {
			if (!BoneSelect.Active) return null;

			var target = Ktisis.GPoseTarget;
			if (target == null) return null;

			var model = ((Actor*)target.Address)->Model;
			if (model == null) return null;

			if (BoneSelect.Child != 0) {
				var children = model->GetChildren();
				model = null;
				foreach (var child in children) {
					if (child == BoneSelect.Child)
						model = (ActorModel*)child;
				}
				if (model == null) return null;
			}

			if (model->Skeleton == null) return null;

			return model->Skeleton->GetBone(BoneSelect.Partial, BoneSelect.Index, BoneSelect.Child != 0);
		}
	}
}
