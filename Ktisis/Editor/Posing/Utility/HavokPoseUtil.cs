using System.Numerics;

using FFXIVClientStructs.FFXIV.Client.Graphics.Render;
using FFXIVClientStructs.Havok;

using Ktisis.Common.Extensions;
using Ktisis.Common.Utility;
using Ktisis.Interop;

namespace Ktisis.Editor.Posing.Utility;

public static class HavokPoseUtil {
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
	
	// Model transform
	
	private readonly static Vector3 MinScale = new(0.1f, 0.1f, 0.1f);
	
	public unsafe static Transform? GetModelTransform(hkaPose* pose, int boneIx) {
		if (pose == null || pose->ModelPose.Data == null || boneIx < 0 || boneIx > pose->ModelPose.Length)
			return null;
		return new Transform(*pose->AccessBoneModelSpace(boneIx, hkaPose.PropagateOrNot.DontPropagate));
	}

	public unsafe static void SetModelTransform(hkaPose* pose, int boneIx, Transform trans) {
		if (pose == null || pose->ModelPose.Data == null || boneIx < 0 || boneIx > pose->ModelPose.Length)
			return;
		
		*pose->AccessBoneModelSpace(boneIx, hkaPose.PropagateOrNot.DontPropagate) = trans.ToHavok();
	}
	
	// World transform
	
	public unsafe static Transform? GetWorldTransform(Skeleton* skele, hkaPose* pose, int boneIx) {
		var model = GetModelTransform(pose, boneIx);
		if (model is null || skele == null)
			return null;

		var modelTrans = new Transform(skele->Transform);
		return model.ModelToWorld(modelTrans);
	}

	public unsafe static void SetWorldTransform(Skeleton* skele, hkaPose* pose, int boneIx, Transform trans) {
		if (skele == null || pose == null || boneIx < 0 || boneIx > pose->ModelPose.Length)
			return;
		
		var modelTrans = new Transform(skele->Transform);
		*pose->AccessBoneModelSpace(boneIx, hkaPose.PropagateOrNot.DontPropagate) = trans.WorldToModel(modelTrans).ToHavok();
	}

	public unsafe static void SetWorldTransform(Skeleton* skele, hkaPose* pose, int boneIx, Matrix4x4 trans)
		=> SetWorldTransform(skele, pose, boneIx, new Transform(trans));
	
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
			
			var access = pose->AccessBoneModelSpace(i, hkaPose.PropagateOrNot.DontPropagate);
			var offset = access->Translation.ToVector3() - sourcePos;
			offset = Vector3.Transform(offset, deltaRot);
			var matrix = GetMatrix(access);
			matrix *= Matrix4x4.CreateFromQuaternion(deltaRot);
			matrix.Translation = deltaPos + sourcePos + offset;
			SetMatrix(access, matrix);
		}
	}
	
	// Bone descendants

	public static bool IsBoneDescendantOf(hkArray<short> indices, int bone, int parent) {
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
