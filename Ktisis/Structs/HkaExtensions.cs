using System.Numerics;
using System.Runtime.InteropServices;

using FFXIVClientStructs.Havok;

using Ktisis.Structs.Bones;

namespace Ktisis.Structs {
	public static class HkaExtensions {
		// hkVector4f

		public static Vector3 ToVector3(this hkVector4f vec) => new Vector3(vec.X, vec.Y, vec.Z);
		public static Vector4 ToVector4(this hkVector4f vec) => new Vector4(vec.X, vec.Y, vec.Z, vec.W);

		public static Vector3 Rotate(this hkVector4f vec, Quaternion rot) => Vector3.Transform(vec.ToVector3(), rot);

		// hkQuaternionf

		public static Quaternion ToQuat(this hkQuaternionf q) => new Quaternion(q.X, q.Y, q.Z, q.W);
		public static hkQuaternionf ToHavok(this Quaternion q) => new hkQuaternionf { X = q.X, Y = q.Y, Z = q.Z, W = q.W };

		// hkaPose

		public static BoneIterator GetBones(this hkaPose pose) => new BoneIterator(pose);
	}
}