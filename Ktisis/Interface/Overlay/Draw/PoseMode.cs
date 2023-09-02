using System.Diagnostics;

using Dalamud.Logging;

using Ktisis.Posing;
using Ktisis.Scene.Objects;
using Ktisis.Scene.Objects.Models;

namespace Ktisis.Interface.Overlay.Draw; 

public class PoseMode {
	public unsafe void Draw(SceneDraw draw, Armature arm) {
		var skeleton = arm.GetSkeleton();
		if (skeleton.IsNullPointer || skeleton.Data->PartialSkeletons == null)
			return;

		var time = new Stopwatch();
		time.Start();

		var boneMap = arm.GetBoneMap();

		var partialCt = skeleton.Data->PartialSkeletonCount;
		for (var p = 0; p < partialCt; p++) {
			var partial = skeleton.Data->PartialSkeletons[0];
			var pose = partial.GetHavokPose(0);
			if (pose == null || pose->Skeleton == null) continue;

			var hkSkeleton = pose->Skeleton;
			for (var i = 0; i < hkSkeleton->Bones.Length; i++) {
				if (!boneMap.TryGetValue((p, i), out var bone) || bone.Flags.HasFlag(ObjectFlags.Hidden))
					continue;

				var trans = PoseEditor.GetWorldTransform(skeleton.Data, pose, i);
				if (trans is not null)
					draw.DotSelection.AddItem(bone, trans.Position);
			}
		}
		
		time.Stop();
		
		if (skeleton.Data->PartialSkeletons[0].GetHavokPose(0)->ModelPose.Length > 100)
			PluginLog.Information($"{time.Elapsed.TotalMilliseconds:00.0000}ms");
	}
}