using System.Numerics;

using FFXIVClientStructs.Havok;
using FFXIVClientStructs.FFXIV.Client.Graphics.Render;

using Ktisis.Structs.Bones;
using Lumina.Excel.GeneratedSheets;
using System.Runtime.CompilerServices;

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

		public unsafe static Bone GetBone(this Skeleton skele, int partial, int bone) => new Bone(&skele, partial, bone);

		// Partial

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