using System.Numerics;
using System.Runtime.InteropServices;

using FFXIVClientStructs.Havok;

using Ktisis.Structs.Bones;

namespace Ktisis.Structs {
	public static class HkaExtensions {
		// hkVector4f Extensions

		public static Vector3 ToVector3(this hkVector4f vec) => new Vector3(vec.X, vec.Y, vec.Z);

		public static Vector3 Rotate(this hkVector4f vec, Quaternion rot) => Vector3.Transform(vec.ToVector3(), rot);

		// hkaPose Extensions

		public static BoneIterator GetBones(this hkaPose pose) => new BoneIterator(pose);
	}
}