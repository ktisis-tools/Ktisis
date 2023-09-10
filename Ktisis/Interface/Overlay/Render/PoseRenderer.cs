using System.Linq;

using ImGuiNET;

using Ktisis.Posing;
using Ktisis.Editing.Modes;
using Ktisis.Scene.Objects.Models;

namespace Ktisis.Interface.Overlay.Render; 

public class PoseRenderer : RendererBase {
	// Draw
	
	public override void OnDraw(GuiOverlay overlay, ModeHandler handler) {
		var items = handler.GetEnumerator().Cast<Armature>();
		foreach (var armature in items)
			DrawArmature(overlay, armature);
	}

	private unsafe void DrawArmature(GuiOverlay overlay, Armature armature) {
		var skeleton = armature.GetSkeleton();
		if (skeleton.IsNullPointer || skeleton.Data->PartialSkeletons == null)
			return;
		
		var drawList = ImGui.GetBackgroundDrawList();
		
		var partialCt = skeleton.Data->PartialSkeletonCount;
		for (var p = 0; p < partialCt; p++) {
			var partial = skeleton.Data->PartialSkeletons[p];
			var pose = partial.GetHavokPose(0);
			if (pose == null || pose->Skeleton == null)
				continue;

			var hkaSkeleton = pose->Skeleton;
			var boneCt = hkaSkeleton->Bones.Length;
			for (var i = 0; i < boneCt; i++) {
				if (armature.GetBoneFromMap(p, i) is not { Visible: true } bone)
					continue;

				var trans = PoseEdit.GetWorldTransform(skeleton.Data, pose, i);
				if (trans is null) continue;
				
				overlay.Selection.AddItem(bone, trans.Position);
				
				// Draw lines to children.

				for (var c = i; c < boneCt; c++) {
					if (hkaSkeleton->ParentIndices[c] != i) continue;

					if (armature.GetBoneFromMap(p, c) is not { Visible: true })
						continue;

					var lineTo = PoseEdit.GetWorldTransform(skeleton.Data, pose, c);
					if (lineTo is not null)
						overlay.DrawLine(drawList, trans.Position, lineTo.Position);
				}
			}
		}
	}
}
