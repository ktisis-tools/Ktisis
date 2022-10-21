using System;

using ImGuiNET;

using Dalamud.Logging;

using Ktisis.Localization;
using Ktisis.Structs;
using Ktisis.Structs.Actor;
using Ktisis.Structs.Bones;

namespace Ktisis.Overlay {
	public static class Skeleton {
		public unsafe static void Draw() {
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
			for (var p = 0; p < skele->PartialSkeletonCount; p++) {
				var partial = skele->PartialSkeletons[p];
				var pose = partial.GetHavokPose(0);
				if (pose == null) continue;

				// Iterate bones
				var bones = pose->GetBones();
				foreach (Bone bone in bones) {
					if (!Ktisis.Configuration.IsBoneVisible(bone))
						continue; // Bone is hidden, move onto the next one.

					var boneName = bone.HkaBone.Name.String;
					var gizmoId = $"{p}_{boneName}";

					// Fetch bone category color & convert world pos to screen

					var boneColor = ImGui.GetColorU32(Ktisis.Configuration.GetCategoryColor(bone));
					Dalamud.GameGui.WorldToScreen(bone.GetWorldPos(model), out var pos);

					// Draw line to bone parent if any
					
					if (bone.ParentIndex > 0) {
						var parent = bones[bone.ParentIndex];

						Dalamud.GameGui.WorldToScreen(parent.GetWorldPos(model), out var posParent);

						var lineThickness = Math.Max(0.01f, Ktisis.Configuration.SkeletonLineThickness / Dalamud.Camera->Camera->InterpDistance * 2f);
						draw.AddLine(pos, posParent, boneColor, lineThickness);
					}

					// Add selectable item

					var item = Selection.AddItem($"{Locale.GetBoneName(boneName)}##{p}", pos, boneColor);
					if (item.IsClicked())
						OverlayWindow.SetGizmoOwner(gizmoId);

					// Bone selection & gizmo

					var gizmo = OverlayWindow.GetGizmo(gizmoId);
					if (gizmo != null) {
						
					}
				}
			}
		}
	}
}