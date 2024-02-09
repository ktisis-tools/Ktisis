using System;
using System.Numerics;

namespace Ktisis.Common.Utility;

public static class MathHelpers {
	public readonly static float Deg2Rad = ((float)Math.PI * 2) / 360;
	public readonly static float Rad2Deg = 360 / ((float)Math.PI * 2);
	
	// https://github.com/aers/FFXIVClientStructs/blob/ada62e7ae60de220d1f950b03ddb8d66e9e10daf/FFXIVClientStructs/FFXIV/Common/Math/Quaternion.cs
	
	public static Quaternion EulerAnglesToQuaternion(this Vector3 vec) {
		var euler = vec.NormalizeAngles() * Deg2Rad;
		
		var halfX = euler.X * 0.5f;
		var cX = MathF.Cos(halfX);
		var sX = MathF.Sin(halfX);

		var halfY = euler.Y * 0.5f;
		var cY = MathF.Cos(halfY);
		var sY = MathF.Sin(halfY);

		var halfZ = euler.Z * 0.5f;
		var cZ = MathF.Cos(halfZ);
		var sZ = MathF.Sin(halfZ);

		var qX = new Quaternion(sX, 0.0f, 0.0f, cX);
		var qY = new Quaternion(0.0f, sY, 0.0f, cY);
		var qZ = new Quaternion(0.0f, 0.0f, sZ, cZ);

		return qZ * qY * qX;
	}
	
	private static float NormalizeAngle(float angle) {
		if (angle > 360f)
			angle = 0 + (angle % 360);
		else if (angle < -float.Epsilon)
			angle = 360 - ((360 - angle) % 360);
		return angle;
	}

	public static Vector3 NormalizeAngles(this Vector3 vec) => new(
		NormalizeAngle(vec.X),
		NormalizeAngle(vec.Y),
		NormalizeAngle(vec.Z)
	);
}
