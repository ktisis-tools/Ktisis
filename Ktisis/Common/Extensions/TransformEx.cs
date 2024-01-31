using System.Numerics;

using FFXIVClientStructs.Havok;

using Ktisis.Common.Utility;

namespace Ktisis.Common.Extensions;

public static class TransformEx {
	// Vector3

	public static Vector3 ModelToWorldPos(this Vector3 target, Transform offset)
		=> Vector3.Transform(target, offset.Rotation) * offset.Scale;

	public static Vector3 WorldToModelPos(this Vector3 target, Transform offset) {
		return Vector3.Transform(
			target - offset.Position,
			Quaternion.Inverse(offset.Rotation)
		) / offset.Scale;
	}
	
	// hkVector4f
	
	public static Vector3 ToVector3(this hkVector4f hkVec) => new(hkVec.X, hkVec.Y, hkVec.Z);
	
	// hkQuaternionf
	
	public static Quaternion ToQuaternion(this hkQuaternionf hkQuat) => new(hkQuat.X, hkQuat.Y, hkQuat.Z, hkQuat.W);

	public static hkQuaternionf ToHavok(this Quaternion quat) => new() { X = quat.X, Y = quat.Y, Z = quat.Z, W = quat.W };
}
