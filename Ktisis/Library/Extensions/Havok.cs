using System;
using System.Numerics;
using System.Collections.Generic;

using FFXIVClientStructs.Havok;
using FFXIVClientStructs.FFXIV.Client.Graphics.Render;

using Ktisis.Posing;

namespace Ktisis.Library.Extensions {
	internal static class Havok {
		// hkVector4f

		internal static Vector3 ToVector3(this hkVector4f vec) => new Vector3(vec.X, vec.Y, vec.Z);
		internal static Vector4 ToVector4(this hkVector4f vec) => new Vector4(vec.X, vec.Y, vec.Z, vec.W);

		internal static hkVector4f ToHavok(this Vector3 v) => new hkVector4f { X = v.X, Y = v.Y, Z = v.Z, W = 1 };

		internal static hkVector4f SetFromVector3(this hkVector4f tar, Vector3 vec) {
			tar.X = vec.X;
			tar.Y = vec.Y;
			tar.Z = vec.Z;
			return tar;
		}

		// hkQuaternionf

		internal static Quaternion ToQuat(this hkQuaternionf q) => new Quaternion(q.X, q.Y, q.Z, q.W);
		internal static hkQuaternionf ToHavok(this Quaternion q) => new hkQuaternionf { X = q.X, Y = q.Y, Z = q.Z, W = q.W };

		internal static hkQuaternionf SetFromQuat(this hkQuaternionf tar, Quaternion q) {
			tar.X = q.X;
			tar.Y = q.Y;
			tar.Z = q.Z;
			tar.W = q.W;
			return tar;
		}

		// hkaPose

		internal unsafe static void SetFromLocalPose(this hkaPose pose, hkArray<hkQsTransformf> localPose, bool setScale = false) {
			for (var i = 1; i < pose.Skeleton->Bones.Length; i++) {
				var parent = pose.ModelPose[pose.Skeleton->ParentIndices[i]];

				var local = localPose[i];
				var model = pose.AccessBoneModelSpace(i, 0);

				model->Translation = (
					parent.Translation.ToVector3()
					+ Vector3.Transform(
						local.Translation.ToVector3(),
						parent.Rotation.ToQuat()
					)
				).ToHavok();
				model->Rotation = (parent.Rotation.ToQuat() * local.Rotation.ToQuat()).ToHavok();
				if (setScale) model->Scale = local.Scale;
			}
		}

		internal static void SetFromLocalPose(this hkaPose pose, bool setScale = false)
			=> pose.SetFromLocalPose(pose.LocalPose, setScale);

		// Skeleton

		internal unsafe static PartialSkeleton GetPartial(this Skeleton skele, int p) => skele.PartialSkeletons[p];

		internal unsafe static Bone GetBone(this Skeleton skele, int partial, int bone) => new Bone(&skele, partial, bone); // this probably shouldn't be used like ever?

		internal static IEnumerable<PartialSkeleton> IterateSkeletons(this Skeleton skeleton) {
			var ct = skeleton.PartialSkeletonCount;
			for (var p = 0; p < ct; p++) {
				var partial = skeleton.GetPartial(p);
				if (partial.IsValid())
					yield return partial;
			}
		}

		internal unsafe static void ParentPartialToRoot(this Skeleton skeleton, int p) {
			var partial = skeleton.PartialSkeletons[p];

			var pose = partial.GetHavokPose(0);
			if (pose == null) return;

			if (partial.ConnectedBoneIndex > -1) {
				var bone = partial.Skeleton->GetBone(p, partial.ConnectedBoneIndex);
				var parent = partial.Skeleton->GetBone(0, partial.ConnectedParentBoneIndex);

				var model = bone.AccessModelSpace();
				var initial = *model;
				*model = *parent.AccessModelSpace();

				bone.PropagateChildren(model, initial.Translation.ToVector3(), model->Rotation.ToQuat());
				bone.PropagateChildren(model, model->Translation.ToVector3(), initial.Rotation.ToQuat());
			}
		}

		internal unsafe static void ForEachBone(this Skeleton skeleton, Action<Bone> callback) {
			foreach (var partial in skeleton.IterateSkeletons())
				partial.ForEachBone(callback);
		}

		// PartialSkeleton

		internal unsafe static bool IsValid(this PartialSkeleton partial) => partial.HavokPoses[0] != 0;

		internal unsafe static int GetIndex(this PartialSkeleton partial) {
			var pose = partial.GetHavokPose(0);
			if (pose == null) return -1;

			var skele = partial.Skeleton;
			for (var i = 0; i < skele->PartialSkeletonCount; i++) {
				var pose2 = skele->PartialSkeletons[i].GetHavokPose(0);
				if (pose2 == null)
					continue;
				else if (pose == pose2)
					return i;
			}

			return -1;
		}

		internal unsafe static void ForEachBone(this PartialSkeleton partial, Action<Bone> callback) {
			var pose = partial.GetHavokPose(0);
			if (pose == null) return;

			var x = partial.GetIndex();
			for (var i = 1; i < pose->Skeleton->Bones.Length; i++) {
				var bone = partial.Skeleton->GetBone(x, i);
				callback.Invoke(bone);
			}
		}
	}
}