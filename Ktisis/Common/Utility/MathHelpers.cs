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

	public static Vector3 ToEulerAngles(this Quaternion q)
		=> Quaternion.Normalize(q).ToEulerRad() * Rad2Deg;

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

	private static Vector3 ToEulerRad(this Quaternion q) {
		var unit = Quaternion.Dot(q, q);
		var test = q.X * q.W - q.Y * q.Z;
		Vector3 v;

		if (test > 0.4995f * unit) {
			v.Y = 2.0f * MathF.Atan2(q.Y, q.X);
			v.X = MathF.PI / 2.0f;
			v.Z = 0.0f;
			MakePositive(ref v);
			return v;
		}

		if (test < -0.4995f * unit) {
			v.Y = -2.0f * MathF.Atan2(q.Y, q.X);
			v.X = -MathF.PI / 2.0f;
			v.Z = 0.0f;
			MakePositive(ref v);
			return v;
		}

		var tmp = new Quaternion(q.W, q.Z, q.X, q.Y);
		v.Y = MathF.Atan2(2.0f * tmp.X * tmp.W + 2.0f * tmp.Y * tmp.Z, 1.0f - 2.0f * (tmp.Z * tmp.Z + tmp.W * tmp.W));
		v.X = MathF.Asin(2.0f * (tmp.X * tmp.Z - tmp.W * tmp.Y));
		v.Z = MathF.Atan2(2.0f * tmp.X * tmp.Y + 2.0f * tmp.Z * tmp.W, 1.0f - 2.0f * (tmp.Y * tmp.Y + tmp.Z * tmp.Z));
		MakePositive(ref v);
		return v;
	}

	private static void MakePositive(ref Vector3 euler) {
		const float t = MathF.PI * 2.0f;
		const float negativeFlip = -0.0001f;
		const float positiveFlip = t - 0.0001f;

		if (euler.X < negativeFlip)
			euler.X += t;
		else if (euler.X > positiveFlip)
			euler.X -= t;

		if (euler.Y < negativeFlip)
			euler.Y += t;
		else if (euler.Y > positiveFlip)
			euler.Y -= t;

		if (euler.Z < negativeFlip)
			euler.Z += t;
		else if (euler.Z > positiveFlip)
			euler.Z -= t;
	}

	public static float Lerp(this float a, float b, float t)
		=> a * (1 - t) + b * t;
}
