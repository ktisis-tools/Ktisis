using System.Numerics;

using FFXIVClientStructs.FFXIV.Client.Graphics.Render;
using FFXIVClientStructs.Havok;

using Ktisis.Common.Extensions;
using Ktisis.Common.Utility;
using Ktisis.Interop;

namespace Ktisis.Editor.Posing;

public static class HavokPosing {
	// Matrix wrappers
	
	private readonly static Alloc<Matrix4x4> Matrix = new(16);

	private unsafe static Matrix4x4 GetMatrix(hkQsTransformf* transform) {
		transform->get4x4ColumnMajor((float*)Matrix.Address);
		return *Matrix.Data;
	}

	private unsafe static void SetMatrix(hkQsTransformf* trans, Matrix4x4 matrix) {
		*Matrix.Data = matrix;
		trans->set((hkMatrix4f*)Matrix.Address);
	}

	public unsafe static Matrix4x4 GetMatrix(hkaPose* pose, int boneIndex) {
		var access = pose->AccessBoneModelSpace(boneIndex, hkaPose.PropagateOrNot.DontPropagate);
		return GetMatrix(access);
	}

	public unsafe static void SetMatrix(hkaPose* pose, int boneIndex, Matrix4x4 matrix) {
		var access = pose->AccessBoneModelSpace(boneIndex, hkaPose.PropagateOrNot.DontPropagate);
		SetMatrix(access, matrix);
	}
	
	// Model transform
	
	public unsafe static Transform? GetModelTransform(hkaPose* pose, int boneIx) {
		if (pose == null || pose->ModelPose.Data == null || boneIx < 0 || boneIx > pose->ModelPose.Length)
			return null;

		var access = pose->AccessBoneModelSpace(boneIx, hkaPose.PropagateOrNot.DontPropagate);
		return new Transform(GetMatrix(access));
	}

	public unsafe static void SetModelTransform(hkaPose* pose, int boneIx, Transform trans) {
		if (pose == null || pose->ModelPose.Data == null || boneIx < 0 || boneIx > pose->ModelPose.Length)
			return;

		var access = pose->AccessBoneModelSpace(boneIx, hkaPose.PropagateOrNot.DontPropagate);
		SetMatrix(access, trans.ComposeMatrix());
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
			var subPose = subPartial.GetHavokPose(0);
			if (subPose == null) continue;

			var rootBone = subPartial.ConnectedBoneIndex;
			var parentBone = subPartial.ConnectedParentBoneIndex;
			if (parentBone != boneIx && !IsBoneDescendantOf(hkaSkele->ParentIndices, parentBone, boneIx))
				continue;
			
			Propagate(subPose, rootBone, sourcePos, deltaPos, deltaRot);
		}
	}

	private unsafe static void Propagate(hkaPose* pose, int boneIx, Vector3 sourcePos, Vector3 deltaPos, Quaternion deltaRot) {
		var hkaSkele = pose->Skeleton;
		for (var i = boneIx; i < hkaSkele->Bones.Length; i++) {
			if (!IsBoneDescendantOf(hkaSkele->ParentIndices, i, boneIx))
				continue;
			
			var matrix = GetMatrix(pose, i);
			var offset = matrix.Translation - sourcePos;
			offset = Vector3.Transform(offset, deltaRot);
			matrix *= Matrix4x4.CreateFromQuaternion(deltaRot);
			matrix.Translation = deltaPos + sourcePos + offset;
			SetMatrix(pose, i, matrix);
		}
	}
	
	// Bone descendants

	private static bool IsBoneDescendantOf(hkArray<short> indices, int bone, int parent) {
		if (parent < 1) return true;
		
		var p = indices[bone];
		while (p != -1) {
			if (p == parent)
				return true;
			p = indices[p];
		}
		return false;
	}
}
