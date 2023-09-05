using System.Numerics;

using FFXIVClientStructs.Havok;
using FFXIVClientStructs.FFXIV.Client.Graphics.Render;

using Ktisis.Common.Utility;

namespace Ktisis.Posing; 

public static class PoseEditor {
	private const hkaPose.PropagateOrNot DontPropagate = hkaPose.PropagateOrNot.DontPropagate;
	
	// Conversion
	
	public static Transform ModelToWorld(Transform model, Transform mul)
		=> new Transform(model.ComposeMatrix() * mul.ComposeMatrix());

	public static Transform WorldToModel(Transform world, Transform mul) {
		Matrix4x4.Invert(mul.ComposeMatrix(), out var invert);
		return new Transform(world.ComposeMatrix() * invert);
	}
	
	// Model transform
	
	private readonly static Vector3 MinScale = new(0.1f, 0.1f, 0.1f);
	
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
		return ModelToWorld(model, modelTrans);
	}

	public unsafe static void SetWorldTransform(Skeleton* skele, hkaPose* pose, int boneIx, Transform trans) {
		if (skele == null || pose == null || boneIx < 0 || boneIx > pose->ModelPose.Length)
			return;
		
		var access = pose->AccessBoneModelSpace(boneIx, DontPropagate);
		if (access == null) return;
		
		var modelTrans = new Transform(skele->Transform);
		*access = WorldToModel(trans, modelTrans).ToHavok();
	}

	public unsafe static void SetWorldTransform(Skeleton* skele, hkaPose* pose, int boneIx, Matrix4x4 trans)
		=> SetWorldTransform(skele, pose, boneIx, new Transform(trans));
	
	// Propagation

	public unsafe static void Propagate(Skeleton* skele, int partialIx, int boneIx, Transform target, Transform initial) {
		var partial = skele->PartialSkeletons[partialIx];
		var pose = partial.GetHavokPose(0);
		if (pose == null || pose->Skeleton == null) return;

		// Calculate transform delta & propagate to children
		
		var sourcePos = target.Position;
		var deltaPos = sourcePos - initial.Position;
		var deltaRot = target.Rotation / initial.Rotation;
		Propagate(pose, boneIx, sourcePos, deltaPos, deltaRot);

		if (partialIx != 0) return;
		
		// Propagate connected partial skeletons

		var hkaSkele = pose->Skeleton;
		for (var p = 1; p < skele->PartialSkeletonCount; p++) {
			var subPartial = skele->PartialSkeletons[p];
			var subPose = subPartial.GetHavokPose(0);
			if (subPose == null) continue;

			var rootBone = subPartial.ConnectedBoneIndex;
			var parentBone = subPartial.ConnectedParentBoneIndex;
			if (IsBoneDescendantOf(hkaSkele->ParentIndices, parentBone, boneIx))
				Propagate(subPose, rootBone, sourcePos, deltaPos, deltaRot);
		}
	}

	private unsafe static void Propagate(hkaPose* pose, int boneIx, Vector3 sourcePos, Vector3 deltaPos, Quaternion deltaRot) {
		var hkaSkele = pose->Skeleton;
		for (var i = boneIx; i < hkaSkele->Bones.Length; i++) {
			if (hkaSkele->ParentIndices[i] != boneIx)
				continue;

			var access = pose->AccessBoneModelSpace(i, DontPropagate);
			
			var trans = new Transform(*access);
			var offset = Vector3.Transform(trans.Position - sourcePos, deltaRot);
			var matrix = trans.ComposeMatrix() * Matrix4x4.CreateFromQuaternion(deltaRot);
			matrix.Translation = sourcePos + deltaPos + offset;
			trans.DecomposeMatrix(matrix);
			*access = trans.ToHavok();

			Propagate(pose, i, sourcePos, deltaPos, deltaRot);
		}
	}
	
	// Bone descendants

	public static bool IsBoneDescendantOf(hkArray<short> indices, int bone, int parent) {
		var p = indices[bone];
		while (p != -1) {
			if (p == parent)
				return true;
            p = indices[p];
		}
		return false;
	}
}
