using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using Dalamud.Utility;

using FFXIVClientStructs.FFXIV.Client.Graphics.Render;
using FFXIVClientStructs.Havok.Animation.Rig;
using FFXIVClientStructs.Havok.Common.Base.Container.Array;
using FFXIVClientStructs.Havok.Common.Base.Math.Matrix;
using FFXIVClientStructs.Havok.Common.Base.Math.QsTransform;
using FFXIVClientStructs.Havok.Common.Base.Math.Quaternion;
using FFXIVClientStructs.Havok.Common.Base.Math.Vector;

using Ktisis.Common.Utility;
using Ktisis.Interop;

namespace Ktisis.Editor.Posing;

public static class HavokPosing {
	// Matrix wrappers
	
	private readonly static Alloc<Matrix4x4> Matrix = new(16);
	private readonly static ConcurrentDictionary<nint, Transform?> _abdomenTransformCache = new();

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

	public unsafe static void CalcCachedAbdomenModelTransform(hkaPose* pose, int boneIndex) {
		var cached = _abdomenTransformCache.GetOrAdd((nint)pose, _ => {
			return GetModelTransform(pose, boneIndex);
		});
		if(cached == null) return;

		var qs = pose->ModelPose.Data + boneIndex;
		qs->Translation = new hkVector4f {
			X = cached.Position.X,
			Y = cached.Position.Y,
			Z = cached.Position.Z,
			W = 0f
		};
		qs->Rotation = new hkQuaternionf {
			X = cached.Rotation.X,
			Y = cached.Rotation.Y,
			Z = cached.Rotation.Z,
			W = cached.Rotation.W
		};
		qs->Scale = new hkVector4f {
			X = cached.Scale.X,
			Y = cached.Scale.Y,
			Z = cached.Scale.Z,
			W = 0f
		};
	}

	private unsafe static void SetCachedAbdomenModelTransform(hkaPose* pose, Transform transform) {
		_abdomenTransformCache[(nint)pose] = transform;
	}

	public static void ClearCachedAbdomenModelTransform() => _abdomenTransformCache.Clear();

	public unsafe static Transform? GetModelTransform(hkaPose* pose, int boneIx) {
		if (pose == null) {
			Ktisis.Log.Error($"GetModelTransform - null hkaPose for boneIx {boneIx}");
			return null;
		}
		if (pose->ModelPose.Data == null) {
			Ktisis.Log.Error($"GetModelTransform - null ModelPose for hkaPose {(uint)pose:X}; boneIx {boneIx}");
			return null;
		}
		if (boneIx < 0 || boneIx >= pose->ModelPose.Length) {
			Ktisis.Log.Error($"GetModelTransform - boneIx {boneIx} out of bounds for modelpose length {pose->ModelPose.Length}");
			return null;
		}

		var qs = pose->ModelPose.Data + boneIx;
		var pos = new Vector3(qs->Translation.X, qs->Translation.Y, qs->Translation.Z);
		var rot = new Quaternion(qs->Rotation.X, qs->Rotation.Y, qs->Rotation.Z, qs->Rotation.W);
		var sca = new Vector3(qs->Scale.X, qs->Scale.Y, qs->Scale.Z);

		return new Transform(pos, rot, sca);
	}

	public unsafe static Transform? GetModelTransformFromSpace(hkaPose* pose, int boneIx) {
		// fallback method to attempt grabbing values from SyncedPoseModelSpace instead
		if (pose == null) {
			Ktisis.Log.Error($"GetModelTransformFromSpace - null hkaPose for boneIx {boneIx}");
			return null;
		}
		var modelSpace = pose->GetSyncedPoseModelSpace();
		if (boneIx < 0 || boneIx >= modelSpace->Length) {
			Ktisis.Log.Error($"GetModelTransformFromSpace - boneIx {boneIx} out of bounds for ModelSpace length {modelSpace->Length}");
			return null;
		}

		var qs = modelSpace->Data[boneIx];
		var pos = new Vector3(qs.Translation.X, qs.Translation.Y, qs.Translation.Z);
		var rot = new Quaternion(qs.Rotation.X, qs.Rotation.Y, qs.Rotation.Z, qs.Rotation.W);
		var sca = new Vector3(qs.Scale.X, qs.Scale.Y, qs.Scale.Z);
		return new Transform(pos, rot, sca);
	}

	public unsafe static void SetModelTransform(hkaPose* pose, int boneIx, Transform trans) {
		if (pose == null || pose->ModelPose.Data == null || boneIx < 0 || boneIx >= pose->ModelPose.Length)
			return;

		var qs = pose->ModelPose.Data + boneIx;
		qs->Translation = new hkVector4f {
			X = trans.Position.X,
			Y = trans.Position.Y,
			Z = trans.Position.Z,
			W = 0f
		};
		qs->Rotation = new hkQuaternionf {
			X = trans.Rotation.X,
			Y = trans.Rotation.Y,
			Z = trans.Rotation.Z,
			W = trans.Rotation.W
		};
		qs->Scale = new hkVector4f {
			X = trans.Scale.X,
			Y = trans.Scale.Y,
			Z = trans.Scale.Z,
			W = 0f
		};

		if (pose->Skeleton->Bones[boneIx].Name.String == "n_hara") {
			SetCachedAbdomenModelTransform(pose, trans);
		}
	}

	public unsafe static Transform? GetLocalTransform(hkaPose* pose, int boneIx) {
		if (pose == null || pose->LocalPose.Data == null || boneIx < 0 || boneIx >= pose->LocalPose.Length)
			return null;

		var qs = pose->LocalPose.Data + boneIx;
		var pos = new Vector3(qs->Translation.X, qs->Translation.Y, qs->Translation.Z);
		var rot = new Quaternion(qs->Rotation.X, qs->Rotation.Y, qs->Rotation.Z, qs->Rotation.W);
		var sca = new Vector3(qs->Scale.X, qs->Scale.Y, qs->Scale.Z);

		return new Transform(pos, rot, sca);
	}

	// Propagation

	public unsafe static void Propagate(Skeleton* skele, int partialIx, int boneIx, Transform target, Transform initial, bool propagatePartials = true) {
		// handles propagating a bone to its children and any affected partial skeletons

		var partial = skele->PartialSkeletons[partialIx];
		var pose = partial.GetHavokPose(0);
		if (pose == null || pose->Skeleton == null) return;

		// Calculate transform delta & propagate to children

		var sourcePos = target.Position;
		var deltaPos = sourcePos - initial.Position;
		var deltaRot = Quaternion.Normalize(target.Rotation / initial.Rotation);
		Propagate(pose, boneIx, sourcePos, deltaPos, deltaRot);

		// bail out if partialIx is non-zero (indicating we're trying Propagate on a partial, which can't have its own partials) or skipping propagatePartials
		if (partialIx != 0 || !propagatePartials) return;

		// Iterate and propagate to connected partial skeletons

		var hkaSkele = pose->Skeleton;
		for (var p = 0; p < skele->PartialSkeletonCount; p++) {
			var subPartial = skele->PartialSkeletons[p];
			if (subPartial.HavokPoses.IsEmpty) continue;

			var subPose = subPartial.GetHavokPose(0);
			if (subPose == null || subPose->Skeleton == null) continue;

			var subSkele = subPose->Skeleton;
			if (!IsMultiRootSkeleton(subSkele->ParentIndices)) {
				// propagate normally if this is a single-binding partial (i.e. hair, face to j_kao)
				var rootBoneIdx = subPartial.ConnectedBoneIndex;
				var parentBoneIdx = subPartial.ConnectedParentBoneIndex;

				// bail out if the bone being manipulated is neither the parent of this skeleton NOR a parent of that parent
				// ex: break on Hair skele if we're manipulating Left Hand; propagate if we're manipulating Head or Neck
				if (parentBoneIdx != boneIx && !IsBoneDescendantOf(hkaSkele->ParentIndices, parentBoneIdx, boneIx)) continue;

				if (rootBoneIdx == -1)
					Ktisis.Log.Debug($"Calling -1 Propagate for skeleton {p}\nManipulated BoneIx: {boneIx} / Name: {hkaSkele->Bones[boneIx].Name.String}");
				Propagate(subPose, rootBoneIdx, sourcePos, deltaPos, deltaRot);
			} else {
				// propagate against each root in a multi-root partial (i.e. j_ex_top_a_l to left arm && j_ex_top_a_r to right arm)
				var multiRoots = GetMultiRoots(subSkele->ParentIndices);
				foreach (var rootIdx in multiRoots) {
					// for each root on the multiroot Partial, try to find its counterpart index in the parent Partial by matching names
					var parentRootIdx = TryGetBoneNameIndex(pose, subSkele->Bones[rootIdx].Name.String);

					// Propagate if either:
					// 1. boneIx being posed refers to the same bone as a rootIdx (ex: left arm on parent Partial)
					// 2. boneIx being posed is the parent of a rootIdx within the parent skeleton (ex: left clavicle on parent Partial)
					var boneIsMultiRoot = hkaSkele->Bones[boneIx].Name.String == subSkele->Bones[rootIdx].Name.String;
					var boneIsParent = parentRootIdx != -1 && IsBoneDescendantOf(hkaSkele->ParentIndices, parentRootIdx, boneIx);
					if (boneIsMultiRoot || boneIsParent) {
						if (rootIdx == -1)
							Ktisis.Log.Debug($"Calling -1 Multi-Root Propagate for skeleton {p}\nManipulated BoneIx: {boneIx} / Name: {hkaSkele->Bones[boneIx].Name.String}");
						Propagate(subPose, rootIdx, sourcePos, deltaPos, deltaRot);
					}
				}
			}
		}
	}

	private unsafe static void Propagate(hkaPose* pose, int boneIx, Vector3 sourcePos, Vector3 deltaPos, Quaternion deltaRot) {
		// handles propagating a bone to its immediate children
		var hkaSkele = pose->Skeleton;
		for (var i = boneIx; i < hkaSkele->Bones.Length; i++) {
			if (!IsBoneDescendantOf(hkaSkele->ParentIndices, i, boneIx)) continue;

			var trans = GetModelTransform(pose, i);
			if (trans == null) {
				List<short> parentIndices = [];
				for (var iter = 0; iter < pose->Skeleton->ParentIndices.Length; iter++)
					parentIndices.Add(pose->Skeleton->ParentIndices[iter]);
				List<string?> boneNames = [];
				for (var iter = 0; iter < pose->Skeleton->Bones.Length; iter++)
					boneNames.Add(pose->Skeleton->Bones[iter].Name.String);

				Ktisis.Log.Error($"HavokPosing.Propagate - null transform returned for pose; boneI {i} boneIx {boneIx}");
				Ktisis.Log.Error($"Pose->Skeleton name: {pose->Skeleton->Name.ToString()}\nParentIndices: {string.Join(", ", parentIndices)}\nBones: {string.Join(", ", boneNames)}");
				Ktisis.Log.Error($"PoseValidity: {pose->CheckPoseValidity()}\nPoseTransformsValidity: {pose->CheckPoseTransformsValidity()}");
				trans = GetModelTransformFromSpace(pose, i); // kooky attempt to fallback to modelspace transform
				if (trans == null)
					continue;
			}

			var scm = Matrix4x4.CreateScale(ClampVector3(trans.Scale));
			var rtm = Matrix4x4.CreateFromQuaternion(Quaternion.Normalize(deltaRot * trans.Rotation));
			var trm = Matrix4x4.CreateTranslation(deltaPos + sourcePos + Vector3.Transform(trans.Position - sourcePos, deltaRot));
			SetModelTransform(pose, i, new Transform(scm * rtm * trm, trans));
		}
	}

	private static Vector3 ClampVector3(Vector3 vector) {
		// use to restrict 0-scaled bones from c+
		var x = (vector.X < 0.001f && vector.X > -0.001f) ? 0.001f : vector.X;
		var y = (vector.Y < 0.001f && vector.Y > -0.001f) ? 0.001f : vector.Y;
		var z = (vector.Z < 0.001f && vector.Z > -0.001f) ? 0.001f : vector.Z;
		return new Vector3(x, y, z);
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
		
		var deltaRot = Quaternion.Normalize(target.Rotation / initial.Rotation);

		var step1 = new Transform(target.Position, initial.Rotation, initial.Scale);
		SetModelTransform(pose, partial.ConnectedBoneIndex, step1);
		Propagate(modelSkeleton, partialIndex, partial.ConnectedBoneIndex, step1, initial);

		var step2 = new Transform(target.Position, Quaternion.Normalize(deltaRot * initial.Rotation), target.Scale);
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
			model.Rotation = Quaternion.Normalize(parent.Rotation * local.Rotation);
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
	// these expect a hkArray<short> ParentIndices from an hkaSkeleton, and evaluate based on whether values in the sklb's bone->parent mapping match -1 to indicate multiple roots

	private static List<int> GetMultiRoots(hkArray<short> indices) {
		List<int> parentIndices = [];
		for(var p = 0; p < indices.Length; p++) {
			if (indices[p] == -1) parentIndices.Add(p);
		}
		return parentIndices;
	}

	private static bool IsMultiRootSkeleton(hkArray<short> indices) => GetMultiRoots(indices).Count > 1;
}
