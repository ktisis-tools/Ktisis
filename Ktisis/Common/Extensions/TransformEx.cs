using System.Numerics;

using FFXIVClientStructs.Havok;

namespace Ktisis.Common.Extensions;

public static class TransformEx {
	// hkVector4f

	public static Vector3 ToVector3(this hkVector4f hkVec)
		=> new Vector3(hkVec.X, hkVec.Y, hkVec.Z);

	public static hkVector4f ToHavok(this Vector3 vec)
		=> new hkVector4f { X = vec.X, Y = vec.Y, Z = vec.Z, W = 1 };

	public static void SetFrom(this hkVector4f hk, Vector3 vec) {
		hk.X = vec.X;
		hk.Y = vec.Y;
		hk.Z = vec.Z;
		hk.W = 1;
	}

	// hkQuaternionf

	public static Quaternion ToQuaternion(this hkQuaternionf hkQuat)
		=> new Quaternion(hkQuat.X, hkQuat.Y, hkQuat.Z, hkQuat.W);

	public static hkQuaternionf ToHavok(this Quaternion quat)
		=> new hkQuaternionf { X = quat.X, Y = quat.Y, Z = quat.Z, W = quat.W };

	public static void SetFrom(this hkQuaternionf hk, Quaternion q) {
		hk.X = q.X;
		hk.Y = q.Y;
		hk.Z = q.Z;
		hk.W = q.W;
	}
}
