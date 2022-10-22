using System.Numerics;
using System.Runtime.InteropServices;

using FFXIVClientStructs.Havok;

using Ktisis.Structs.Bones;

namespace Ktisis.Structs {
	public static class HkaExtensions {
		// hkVector4f

		public static Vector3 ToVector3(this hkVector4f vec) => new Vector3(vec.X, vec.Y, vec.Z);

		public static Vector3 Rotate(this hkVector4f vec, Quaternion rot) => Vector3.Transform(vec.ToVector3(), rot);

		// hkQuaternionf

		public static Quaternion ToQuat(this hkQuaternionf quat) => new Quaternion(quat.X, quat.Y, quat.Z, quat.W);

		// hkaPose

		public static BoneIterator GetBones(this hkaPose pose) => new BoneIterator(pose);
	}
}