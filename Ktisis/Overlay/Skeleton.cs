using ImGuiNET;

using Dalamud.Logging;

using Ktisis.Structs;
using Ktisis.Structs.Actor;
using Ktisis.Structs.Bones;

namespace Ktisis.Overlay {
	public class Skeleton {
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
			for (var p = 0; p < skele->PartialSkeletonCount; p++) {
				var partial = skele->PartialSkeletons[p];
				var pose = partial.GetHavokPose(0);
				if (pose == null) continue;

				var bones = pose->GetBones();
				foreach (Bone bone in bones) {
					Dalamud.GameGui.WorldToScreen(bone.GetWorldPos(model), out var pos);
					
					if (bone.ParentIndex > 0) {
						var parent = bones[bone.ParentIndex];
						Dalamud.GameGui.WorldToScreen(parent.GetWorldPos(model), out var posParent);
						draw.AddLine(pos, posParent, 0xffffffff, 2f);
					}

					draw.AddCircleFilled(pos, 5f, 0xffffffff, 100);
				}
			}
		}
	}
}