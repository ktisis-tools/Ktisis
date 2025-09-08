using System.Collections.Generic;
using System.Linq;

using FFXIVClientStructs.FFXIV.Client.Graphics.Render;
using FFXIVClientStructs.Havok.Animation.Rig;

using Ktisis.Scene.Decor;

namespace Ktisis.Editor.Posing.Data;

public class SkeletonPoseData {
	public unsafe Skeleton* Skeleton;
	public PartialSkeleton Partial;
	public unsafe hkaPose* Pose;

	public unsafe short TryResolveBone(IEnumerable<string> names) => names
		.Select(name => HavokPosing.TryGetBoneNameIndex(this.Pose, name))
		.FirstOrDefault(index => index != -1, (short)-1);

	public unsafe static SkeletonPoseData? TryGet(Skeleton* skeleton, int partialIndex, int poseIndex) {
		if (skeleton == null || skeleton->PartialSkeletons == null || partialIndex > skeleton->PartialSkeletonCount)
			return null;

		var partial = skeleton->PartialSkeletons[partialIndex];
		if (partial.HavokPoses.IsEmpty || partial.SkeletonResourceHandle == null)
			return null;
		
		var pose = partial.GetHavokPose(poseIndex);
		if (pose == null || pose->Skeleton == null)
			return null;

		return new SkeletonPoseData {
			Skeleton = skeleton,
			Partial = partial,
			Pose = pose
		};
	}

	public unsafe static SkeletonPoseData? TryGet(ISkeleton skeleton, int partialIndex, int poseIndex) {
		var ptr = skeleton.GetSkeleton();
		return ptr != null ? TryGet(ptr, partialIndex, poseIndex) : null;
	}
}
