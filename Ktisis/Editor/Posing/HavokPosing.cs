using System.Numerics;
using System.Collections.Generic;

using Dalamud.Utility;

using FFXIVClientStructs.FFXIV.Client.Graphics.Render;
using FFXIVClientStructs.Havok;

using Ktisis.Common.Extensions;
using Ktisis.Common.Utility;
using Ktisis.Interop;

namespace Ktisis.Editor.Posing;

public static class HavokPosing {
	// Matrix wrappers
	
	private readonly static Alloc<Matrix4x4> Matrix = new(16);

	public unsafe static Matrix4x4 GetMatrix(hkQsTransformf* transform) {
		transform->get4x4ColumnMajor((float*)Matrix.Address);
		return *Matrix.Data;
	}
	
	public unsafe static Matrix4x4 GetMatrix(hkaPose* pose, int boneIndex) {
		if (pose == null || pose->ModelPose.Data == null)
			return Matrix4x4.Identity;
		return GetMatrix(pose->ModelPose.Data + boneIndex);
	}

	public unsafe static void SetMatrix(hkQsTransformf* trans, Matrix4x4 matrix) {
		*Matrix.Data = matrix;
		trans->set((hkMatrix4f*)Matrix.Address);
	}

	public unsafe static void SetMatrix(hkaPose* pose, int boneIndex, Matrix4x4 matrix) {
		SetMatrix(pose->ModelPose.Data + boneIndex, matrix);
	}
	
	// Model transform
	
	public unsafe static Transform? GetModelTransform(hkaPose* pose, int boneIx) {
		if (pose == null || pose->ModelPose.Data == null || boneIx < 0 || boneIx > pose->ModelPose.Length)
			return null;
		return new Transform(GetMatrix(pose->ModelPose.Data + boneIx));
	}

	public unsafe static void SetModelTransform(hkaPose* pose, int boneIx, Transform trans) {
		if (pose == null || pose->ModelPose.Data == null || boneIx < 0 || boneIx > pose->ModelPose.Length)
			return;
		SetMatrix(pose->ModelPose.Data + boneIx, trans.ComposeMatrix());
	}

	public unsafe static Transform? GetLocalTransform(hkaPose* pose, int boneIx) {
		if (pose == null || pose->LocalPose.Data == null || boneIx < 0 || boneIx > pose->LocalPose.Length)
			return null;
		return new Transform(GetMatrix(pose->LocalPose.Data + boneIx));
	}
	
	// Propagation

	public unsafe static void Propagate(Skeleton* skele, int partialIx, int boneIx, Transform target, Transform initial, bool propagatePartials = true) {
		var partial = skele->PartialSkeletons[partialIx];
		var pose = partial.GetHavokPose(0);
		if (pose == null || pose->Skeleton == null) return;

		// Calculate transform delta & propagate to children
		
		var sourcePos = target.Position;
		var deltaPos = sourcePos - initial.Position;
		var deltaRot = target.Rotation / initial.Rotation;
		Propagate(pose, boneIx, sourcePos, deltaPos, deltaRot);

		if (partialIx != 0 || !propagatePartials) return;
		
		// Propagate connected partial skeletons

		var hkaSkele = pose->Skeleton;
		for (var p = 0; p < skele->PartialSkeletonCount; p++) {
			var subPartial = skele->PartialSkeletons[p];
			if (subPartial.HavokPoses == null) continue;

			var subPose = subPartial.GetHavokPose(0);
			if (subPose == null) continue;

			var subSkele = subPose->Skeleton;
			if (!IsMultiRootSkeleton(subSkele->ParentIndices)) {
				// propagate normally if this is a single-binding partial (i.e. hair, face to j_kao)
				var rootBone = subPartial.ConnectedBoneIndex;
				var parentBone = subPartial.ConnectedParentBoneIndex;
				if (parentBone != boneIx && !IsBoneDescendantOf(hkaSkele->ParentIndices, parentBone, boneIx)) continue;
				Propagate(subPose, rootBone, sourcePos, deltaPos, deltaRot);
			} else {
				// propagate against each root in a multi-root partial (i.e. j_ex_top_a_l to j_ude_a_l && j_ex_top_a_r to j_ude_a_r)
				var multi_roots = GetMultiRoots(subSkele->ParentIndices);
				foreach(int root_idx in multi_roots) {
					var parent_root_idx = TryGetBoneNameIndex(pose, subSkele->Bones[root_idx].Name.String);

					// account for either:
					// 1. boneIx being posed refers to the same bone as a root_idx
					// 2. boneIx being posed is the parent of a root_idx within the parent skeleton
					bool manipulated_bone_is_multi_root = hkaSkele->Bones[boneIx].Name.String == subSkele->Bones[root_idx].Name.String;
					bool manipulated_bone_is_parent = parent_root_idx != -1 ? IsBoneDescendantOf(hkaSkele->ParentIndices, parent_root_idx, boneIx) : false;
					if (manipulated_bone_is_multi_root || manipulated_bone_is_parent) Propagate(subPose, root_idx, sourcePos, deltaPos, deltaRot);
				}
			}
		}
	}

	private unsafe static void Propagate(hkaPose* pose, int boneIx, Vector3 sourcePos, Vector3 deltaPos, Quaternion deltaRot) {
		var hkaSkele = pose->Skeleton;
		for (var i = boneIx; i < hkaSkele->Bones.Length; i++) {
			if (!IsBoneDescendantOf(hkaSkele->ParentIndices, i, boneIx)) continue;

			var trans = GetModelTransform(pose, i)!;
			var scm = Matrix4x4.CreateScale(trans.Scale);
			var rtm = Matrix4x4.CreateFromQuaternion(deltaRot * trans.Rotation);
			var trm = Matrix4x4.CreateTranslation(deltaPos + sourcePos + Vector3.Transform(trans.Position - sourcePos, deltaRot));
			SetMatrix(pose, i, scm * rtm * trm);
		}
	}
	
	// Parenting
	
	public unsafe static Quaternion ParentSkeleton(
		Skeleton* modelSkeleton,
		int partialIndex
	) {
		var partial = modelSkeleton->PartialSkeletons[partialIndex];
		var pose = partial.GetHavokPose(0);
		if (pose == null) return Quaternion.Identity;
		
		var rootPartial = modelSkeleton->PartialSkeletons[0];
		var rootPose = rootPartial.GetHavokPose(0);
		if (rootPose == null) return Quaternion.Identity;

		var initial = GetModelTransform(pose, partial.ConnectedBoneIndex)!;
		var target = GetModelTransform(rootPose, partial.ConnectedParentBoneIndex)!;
		
		var deltaRot = target.Rotation / initial.Rotation;

		var step1 = new Transform(target.Position, initial.Rotation, initial.Scale);
		SetModelTransform(pose, partial.ConnectedBoneIndex, step1);
		Propagate(modelSkeleton, partialIndex, partial.ConnectedBoneIndex, step1, initial);

		var step2 = new Transform(target.Position, deltaRot * initial.Rotation, target.Scale);
		SetModelTransform(pose, partial.ConnectedBoneIndex, step2);
		Propagate(modelSkeleton, partialIndex, partial.ConnectedBoneIndex, step2, step1);
		
		return deltaRot;
	}
	
	// Base havok utilities

	public unsafe static void SyncModelSpace(Skeleton* skeleton, int partialIndex) {
		if (skeleton == null || skeleton->PartialSkeletons == null) return;

		var partial = skeleton->PartialSkeletons[partialIndex];
		var pose = partial.GetHavokPose(0);
		if (pose == null || pose->Skeleton == null) return;
		
		for (var i = 1; i < pose->Skeleton->Bones.Length; i++) {
			var parent = GetModelTransform(pose, pose->Skeleton->ParentIndices[i]);
			if (parent == null) continue;

			var local = GetLocalTransform(pose, i)!;
			var model = GetModelTransform(pose, i)!;

			model.Position = parent.Position + Vector3.Transform(local.Position, parent.Rotation);
			model.Rotation = parent.Rotation * local.Rotation;
			SetModelTransform(pose, i, model);
		}
		
		if (partialIndex > 0)
			ParentSkeleton(skeleton, partialIndex);
	}
	
	// Lookup

	public unsafe static short TryGetBoneNameIndex(hkaPose* pose, string? name) {
		if (pose == null || pose->Skeleton == null || name.IsNullOrEmpty())
			return -1;

		var bones = pose->Skeleton->Bones;
		for (short i = 0; i < bones.Length; i++) {
			if (bones[i].Name.String == name)
				return i;
		}
		
		return -1;
	}
	
	// Bone descendants

	public static bool IsBoneDescendantOf(hkArray<short> indices, int bone, int parent) {
		// only shortcut out of descendant evaluation if this is a single-root skeleton,
		// and parent is the 0 index
		if (!IsMultiRootSkeleton(indices) && parent < 1) return true;
		
		var p = indices[bone];
		while (p != -1) {
			if (p == parent)
				return true;
			p = indices[p];
		}
		return false;
	}

	// Helpers for multi-binding partials
	public static bool IsMultiRootSkeleton(hkArray<short> indices) {
		if (GetMultiRoots(indices).Count > 1) return true;
		return false;
	}

	public static List<int> GetMultiRoots(hkArray<short> indices) {
		List<int> parent_indices = new();
		for(var p = 0; p < indices.Length; p++) {
			if (indices[p] == -1) parent_indices.Add(p);
		}
		return parent_indices;
	}
}
