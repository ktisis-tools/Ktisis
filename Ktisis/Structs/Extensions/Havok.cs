using System.Numerics;

using FFXIVClientStructs.FFXIV.Client.Graphics.Render;
using FFXIVClientStructs.Havok.Animation.Rig;
using FFXIVClientStructs.Havok.Common.Base.Container.Array;
using FFXIVClientStructs.Havok.Common.Base.Math.QsTransform;
using FFXIVClientStructs.Havok.Common.Base.Math.Quaternion;
using FFXIVClientStructs.Havok.Common.Base.Math.Vector;

using Ktisis.Structs.Bones;

namespace Ktisis.Structs {
	public static class Havok {
		// hkVector4f

		public static Vector3 ToVector3(this hkVector4f vec) => new Vector3(vec.X, vec.Y, vec.Z);
		public static Vector4 ToVector4(this hkVector4f vec) => new Vector4(vec.X, vec.Y, vec.Z, vec.W);

		public static hkVector4f ToHavok(this Vector3 v) => new hkVector4f { X = v.X, Y = v.Y, Z = v.Z, W = 1 };

		public static hkVector4f SetFromVector3(this hkVector4f tar, Vector3 vec) {
			tar.X = vec.X;
			tar.Y = vec.Y;
			tar.Z = vec.Z;
			return tar;
		}

		// hkQuaternionf

		public static Quaternion ToQuat(this hkQuaternionf q) => new Quaternion(q.X, q.Y, q.Z, q.W);
		public static hkQuaternionf ToHavok(this Quaternion q) => new hkQuaternionf { X = q.X, Y = q.Y, Z = q.Z, W = q.W };

		public static hkQuaternionf SetFromQuat(this hkQuaternionf tar, Quaternion q) {
			tar.X = q.X;
			tar.Y = q.Y;
			tar.Z = q.Z;
			tar.W = q.W;
			return tar;
		}

		// hkaPose

		public unsafe static void SetFromLocalPose(this hkaPose pose, hkArray<hkQsTransformf> localPose, bool setScale = false) {
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

		public static void SetFromLocalPose(this hkaPose pose, bool setScale = false)
			=> pose.SetFromLocalPose(pose.LocalPose, setScale);

		// Skeleton

		public unsafe static Bone GetBone(this Skeleton skele, int partial, int bone, bool isChild = false) => new Bone(&skele, partial, bone, isChild);

		public unsafe static void ParentPartialToRoot(this Skeleton skeleton, int p) {
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
	}
}
