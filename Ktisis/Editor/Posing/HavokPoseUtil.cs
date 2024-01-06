using System.Numerics;

using FFXIVClientStructs.FFXIV.Client.Graphics.Render;
using FFXIVClientStructs.Havok;

using Ktisis.Common.Utility;

namespace Ktisis.Editor.Posing;

public static class HavokPoseUtil {
	private const hkaPose.PropagateOrNot DontPropagate = hkaPose.PropagateOrNot.DontPropagate;
	
	// Model transform
	
	private static readonly Vector3 MinScale = new(0.1f, 0.1f, 0.1f);
	
	public unsafe static Transform? GetModelTransform(hkaPose* pose, int boneIx) {
		if (pose == null || pose->ModelPose.Data == null || boneIx < 0 || boneIx > pose->ModelPose.Length)
			return null;
		return new Transform(pose->ModelPose[boneIx]);
	}

	public unsafe static void SetModelTransform(hkaPose* pose, int boneIx, Transform trans) {
		if (pose == null || pose->ModelPose.Data == null || boneIx < 0 || boneIx > pose->ModelPose.Length)
			return;

		var access = pose->AccessBoneModelSpace(boneIx, DontPropagate);
		if (access == null) return;

		trans.Scale = Vector3.Max(trans.Scale, MinScale);

		*access = trans.ToHavok();
	}
	
	public unsafe static void SetModelTransform(Skeleton* skele, hkaPose* pose, int boneIx, Matrix4x4 trans)
		=> SetModelTransform(pose, boneIx, new Transform(trans));
	
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
		
		var access = pose->AccessBoneModelSpace(boneIx, DontPropagate);
		if (access == null) return;
		
		var modelTrans = new Transform(skele->Transform);
		*access = trans.WorldToModel(modelTrans).ToHavok();
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
			
			if (boneIx == parentBone)
				SetModelTransform(subPose, rootBone, target);
			Propagate(subPose, rootBone, sourcePos, deltaPos, deltaRot);
		}
	}

	private unsafe static void Propagate(hkaPose* pose, int boneIx, Vector3 sourcePos, Vector3 deltaPos, Quaternion deltaRot) {
		var hkaSkele = pose->Skeleton;
		for (var i = boneIx; i < hkaSkele->Bones.Length; i++) {
			if (!IsBoneDescendantOf(hkaSkele->ParentIndices, i, boneIx))
				continue;

			var access = pose->AccessBoneModelSpace(i, DontPropagate);
			
			var trans = new Transform(*access);
			var offset = Vector3.Transform(trans.Position - sourcePos, deltaRot);
			var matrix = trans.ComposeMatrix() * Matrix4x4.CreateFromQuaternion(deltaRot);
			matrix.Translation = sourcePos + deltaPos + offset;
			trans.DecomposeMatrix(matrix);
			*access = trans.ToHavok();
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
