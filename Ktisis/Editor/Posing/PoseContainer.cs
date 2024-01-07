using System;
using System.Linq;
using System.Numerics;
using System.Collections.Generic;

using Dalamud.Utility;

using FFXIVClientStructs.Havok;
using FFXIVClientStructs.FFXIV.Client.Graphics.Render;

using Ktisis.Common.Utility;
using Ktisis.Common.Extensions;
using Ktisis.Editor.Posing.Partials;

namespace Ktisis.Editor.Posing;

[Flags]
public enum PoseTransforms {
	None = 0,
	Rotation = 1,
	Position = 2,
	Scale = 4
}

[Flags]
public enum PoseMode {
	None = 0,
	Body = 1,
	Face = 2,
	BodyFace = Body | Face,
	Weapons = 4,
	All = BodyFace | Weapons
}

[Serializable]
public class PoseContainer : Dictionary<string, Transform> {
	public unsafe void Store(
		Skeleton* modelSkeleton
	) {
		if (modelSkeleton == null) return;
		
		this.Clear();

		var partialCt = modelSkeleton->PartialSkeletonCount;
		var partials = modelSkeleton->PartialSkeletons;
		for (var p = 0; p < partialCt; p++) {
			var partial = partials[p];

			var pose = partial.GetHavokPose(0);
			if (pose == null || pose->Skeleton == null) continue;

			var skeleton = pose->Skeleton;
			for (var i = 0; i < skeleton->Bones.Length; i++) {
				if (i == partial.ConnectedBoneIndex) continue;

				var name = skeleton->Bones[i].Name.String;
				if (name.IsNullOrEmpty()) continue;
				
				this[name] = new Transform(pose->ModelPose[i]);
			}
		}
	}

	public unsafe void Apply(
		Skeleton* modelSkeleton,
		PoseTransforms transforms = PoseTransforms.Rotation
	) {
		if (modelSkeleton == null) return;
		for (var p = 0; p < modelSkeleton->PartialSkeletonCount; p++)
			this.ApplyToPartial(modelSkeleton, p, transforms);
	}

	public unsafe void ApplyToBones(
		Skeleton* modelSkeleton,
		IEnumerable<PartialBoneInfo> bones,
		PoseTransforms transforms = PoseTransforms.Rotation
	) {
		var boneMap = new Dictionary<int, List<int>>();
		
		foreach (var bone in bones) {
			var key = bone.PartialIndex;
			if (!boneMap.TryGetValue(key, out var boneList)) {
				boneList = [];
				boneMap.Add(key, boneList);
			}
			boneList.Add(bone.BoneIndex);
		}

		for (var index = 0; index < modelSkeleton->PartialSkeletonCount; index++) {
			if (!boneMap.TryGetValue(index, out var boneList)) continue;
			this.ApplyToPartialBones(modelSkeleton, index, boneList, transforms);
		}
	}

	public unsafe void ApplyToPartial(
		Skeleton* modelSkeleton,
		int partialIndex,
		PoseTransforms transforms = PoseTransforms.Rotation
	) {
		var partial = modelSkeleton->PartialSkeletons[partialIndex];
		var pose = partial.GetHavokPose(0);
		if (pose == null || pose->Skeleton == null) return;
		
		this.ApplyToPartialBones(
			modelSkeleton,
			partialIndex,
			Enumerable.Range(partialIndex > 0 ? 0 : 1, pose->Skeleton->Bones.Length),
			transforms
		);
	}

	public unsafe void ApplyToPartialBones(
		Skeleton* modelSkeleton,
		int partialIndex,
		IEnumerable<int> bones,
		PoseTransforms transforms = PoseTransforms.Rotation
	) {
		if (modelSkeleton == null) return;
		
		var partial = modelSkeleton->PartialSkeletons[partialIndex];
		var pose = partial.GetHavokPose(0);
		if (pose == null || pose->Skeleton == null) return;

		var skeleton = pose->Skeleton;

		// Parent root of partial skeleton & calculate rotation delta
		var offset = Quaternion.Identity;
		if (partialIndex > 0) {
			this.ParentSkeleton(modelSkeleton, partial, pose);
			
			var rootIx = partial.ConnectedBoneIndex;
			var rotation = pose->ModelPose[rootIx].Rotation.ToQuaternion();
			
			var parentName = skeleton->Bones[rootIx].Name.String;
			if (!parentName.IsNullOrEmpty() && this.TryGetValue(parentName, out var parent))
				offset = rotation / parent.Rotation;
		}

		var range = Enumerable.Range(partialIndex > 0 ? 1 : 0, skeleton->Bones.Length);
		foreach (var i in range.Intersect(bones))
			this.ApplyToBone(modelSkeleton, pose, partialIndex, i, offset, transforms);
	}
	
	public unsafe void ApplyToBone(
		Skeleton* modelSkeleton,
		hkaPose* pose,
		int partialIndex,
		int boneIndex,
		Quaternion offset,
		PoseTransforms transforms = PoseTransforms.Rotation
	) {
		var name = pose->Skeleton->Bones[boneIndex].Name.String;
		if (name.IsNullOrEmpty()) return;

		if (!this.TryGetValue(name, out var model)) return;
		
		var initial = HavokPoseUtil.GetModelTransform(pose, boneIndex)!;

		var target = new Transform(initial.Position, initial.Rotation, initial.Scale);
		if (transforms.HasFlag(PoseTransforms.Rotation))
			target.Rotation = offset * model.Rotation;
		
		HavokPoseUtil.SetModelTransform(pose, boneIndex, target);
		HavokPoseUtil.Propagate(modelSkeleton, partialIndex, boneIndex, target, initial);
	}

	private unsafe void ParentSkeleton(
		Skeleton* modelSkeleton,
		PartialSkeleton partial,
		hkaPose* pose
	) {
		var rootPartial = modelSkeleton->PartialSkeletons[0];
		var rootPose = rootPartial.GetHavokPose(0);
		if (rootPose == null) return;

		var parent = rootPose->ModelPose[partial.ConnectedParentBoneIndex];
		HavokPoseUtil.SetModelTransform(pose, partial.ConnectedBoneIndex, new Transform(parent));
	}
}
