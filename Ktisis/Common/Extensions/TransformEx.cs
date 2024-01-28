using System.Numerics;

using FFXIVClientStructs.Havok;

namespace Ktisis.Common.Extensions;

public static class TransformEx {
	// hkVector4f
	
	public static Vector3 ToVector3(this hkVector4f hkVec) => new(hkVec.X, hkVec.Y, hkVec.Z);
	
	// hkQuaternionf
	
	public static Quaternion ToQuaternion(this hkQuaternionf hkQuat) => new(hkQuat.X, hkQuat.Y, hkQuat.Z, hkQuat.W);
}
