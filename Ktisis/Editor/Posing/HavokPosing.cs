using System.Numerics;

using Dalamud.Utility;

using FFXIVClientStructs.FFXIV.Client.Graphics.Render;
using FFXIVClientStructs.Havok;

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
