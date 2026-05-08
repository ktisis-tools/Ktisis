using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using Dalamud.Utility;

using FFXIVClientStructs.FFXIV.Client.Graphics.Render;
using FFXIVClientStructs.Havok.Animation.Rig;

using Ktisis.Common.Extensions;
using Ktisis.Common.Utility;
using Ktisis.Editor.Posing.Types;

namespace Ktisis.Editor.Posing.Data;

[Flags]
public enum PoseTransforms {
	None = 0,
	Rotation = 1,
	Position = 2,
	Scale = 4,
	PositionRoot = 8
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
		Skeleton* modelSkeleton,
		PoseContainer? filter = null
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
				if(filter != null && (!filter.ContainsKey(name) || p == 4)) continue;
				this[name] = new Transform(pose->ModelPose[i]);
			}
		}
	}

	public unsafe void Apply(
		Skeleton* modelSkeleton,
		PoseMode modes = PoseMode.All,
		PoseTransforms transforms = PoseTransforms.Rotation
	) {
		if (modelSkeleton == null) return;
		if (!modes.HasFlag(PoseMode.Face) && !modes.HasFlag(PoseMode.Body)) return;
		for (var p = 0; p < modelSkeleton->PartialSkeletonCount; p++) {
			switch (p) {
				case 1 or 2:
					if (!modes.HasFlag(PoseMode.Face))
						continue;
					break;
				default:
					if (!modes.HasFlag(PoseMode.Body))
						continue;
					break;
			}
			this.ApplyToPartial(modelSkeleton, p, transforms, modes);
		}
	}

	public unsafe void ApplyToBones(
		Skeleton* modelSkeleton,
		IEnumerable<PartialBoneInfo> bones,
		PoseTransforms transforms = PoseTransforms.Rotation,
		PoseMode modes = PoseMode.All
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
			this.ApplyToPartialBones(modelSkeleton, index, boneList, transforms, modes, true);
		}
	}

	public unsafe void ApplyToPartial(
		Skeleton* modelSkeleton,
		int partialIndex,
		PoseTransforms transforms = PoseTransforms.Rotation,
		PoseMode modes = PoseMode.All
	) {
		var partial = modelSkeleton->PartialSkeletons[partialIndex];
		var pose = partial.GetHavokPose(0);
		if (pose == null || pose->Skeleton == null) return;
		
		this.ApplyToPartialBones(
			modelSkeleton,
			partialIndex,
			Enumerable.Range(1, pose->Skeleton->Bones.Length - 1),
			transforms,
			modes
		);
	}

	public unsafe void ApplyToPartialBones(
		Skeleton* modelSkeleton,
		int partialIndex,
		IEnumerable<int> bones,
		PoseTransforms transforms = PoseTransforms.Rotation,
		PoseMode modes = PoseMode.All,
		bool isSelective = false
	) {
		if (modelSkeleton == null) return;
		
		var partial = modelSkeleton->PartialSkeletons[partialIndex];
		var pose = partial.GetHavokPose(0);
		if (pose == null || pose->Skeleton == null) return;

		var skeleton = pose->Skeleton;

		// Parent root of partial skeleton & calculate rotation delta
		var offset = Quaternion.Identity;
		if (partialIndex > 0) {
			var delta = HavokPosing.ParentSkeleton(modelSkeleton, partialIndex);
			
			var rootIx = partial.ConnectedBoneIndex;
			var rotation = pose->ModelPose[rootIx].Rotation.ToQuaternion();
			
			var parentName = skeleton->Bones[rootIx].Name.String;
			if (!parentName.IsNullOrEmpty() && this.TryGetValue(parentName, out var parent))
				offset = rotation / parent.Rotation / delta;
			else
				Ktisis.Log.Warning($"Failed to find parent bone '{parentName}' for partial {partialIndex}!");
		}
		
		var range = Enumerable.Range(1, skeleton->Bones.Length - 1);
		foreach (var i in range.Intersect(bones))
			this.ApplyToBone(modelSkeleton, pose, partialIndex, i, offset, transforms, modes, isSelective);
	}
	
	public unsafe void ApplyToBone(
		Skeleton* modelSkeleton,
		hkaPose* pose,
		int partialIndex,
		int boneIndex,
		Quaternion offset,
		PoseTransforms transforms = PoseTransforms.Rotation,
		PoseMode modes = PoseMode.All,
		bool isSelective = false
	) {
		var name = pose->Skeleton->Bones[boneIndex].Name.String;
		if (name.IsNullOrEmpty()) return;

		if (!this.TryGetValue(name, out var model)) return;
		
		var initial = HavokPosing.GetModelTransform(pose, boneIndex)!;
		var target = new Transform(initial.Position, initial.Rotation, initial.Scale);

		var applyRotation = transforms.HasFlag(PoseTransforms.Rotation);
		var applyPosition = transforms.HasFlag(PoseTransforms.Position);
		var applyScale = transforms.HasFlag(PoseTransforms.Scale);
		var posRoot = partialIndex == 0 && boneIndex == 1 && transforms.HasFlag(PoseTransforms.PositionRoot);

		// sanitize posroot (e.g. n_hara) rotation from file - this fixes weird quaternion propagation bugs
		if (partialIndex == 0 && boneIndex == 1)
			model.Rotation = Quaternion.Identity;

		if (posRoot)
			initial.Rotation = Quaternion.Normalize(offset * model.Rotation);
		if (applyScale)
			target.Scale = model.Scale;

		Transform modelParent = new();
		Transform currentParent = new();
		var applyRelative = isSelective ? (applyPosition || applyRotation) : applyPosition && modes.HasFlag(PoseMode.Face) && !modes.HasFlag(PoseMode.Body);
		var relativeParent = applyRelative && TryGetRelativeParent(pose, partialIndex, boneIndex, out modelParent, out currentParent);

		if (applyPosition || posRoot) {
			if (relativeParent) {
				var localPos = model.Position - modelParent.Position;
				var rotDelta = Quaternion.Normalize(currentParent.Rotation * Quaternion.Inverse(modelParent.Rotation));
				target.Position = currentParent.Position + Vector3.Transform(localPos, rotDelta);
			} else {
				target.Position = model.Position;
			}
		}

		if (applyRotation) {
			if (isSelective && relativeParent) {
				var localRot = Quaternion.Normalize(Quaternion.Inverse(modelParent.Rotation) * model.Rotation);
				target.Rotation = Quaternion.Normalize(currentParent.Rotation * localRot);
			} else {
				target.Rotation = Quaternion.Normalize(offset * model.Rotation);
			}
		}

		HavokPosing.SetModelTransform(pose, boneIndex, target);
		HavokPosing.Propagate(modelSkeleton, partialIndex, boneIndex, target, initial);
	}

	private unsafe bool TryGetRelativeParent(
		hkaPose* pose, 
		int partialIndex, 
		int boneIndex, 
		out Transform modelParent, 
		out Transform currentParent
	) {
		modelParent = new();
		currentParent = new();
		var parentIndex = -1;

		// face/hair relative root j_kao
		if (partialIndex is 1 or 2) {
			parentIndex = 0;
		} else {
			parentIndex = pose->Skeleton->ParentIndices[boneIndex];
			if (parentIndex == -1)
				return false;
		}

		var parentName = pose->Skeleton->Bones[parentIndex].Name.String;
		if(parentName.IsNullOrEmpty() || !this.TryGetValue(parentName, out modelParent!))
			return false;

		currentParent = HavokPosing.GetModelTransform(pose, parentIndex)!;
		return true;
	}
}
