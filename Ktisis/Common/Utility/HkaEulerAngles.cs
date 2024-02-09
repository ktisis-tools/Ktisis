using System;
using System.Numerics;

namespace Ktisis.Common.Utility;

internal static class HkaEulerAngles {
	// Degrees <=> Radians

	internal const float Deg2Rad = (MathF.PI * 2) / 360;
	internal const float Rad2Deg = 360 / (MathF.PI * 2);

	// Quaternion <=> Euler
	
	// https://github.com/lmcintyre/fbx2havok/blob/master/Core/EulerAngles.h
	// Graphics Gems 4 - III.5. - Euler Angle Conversion (Ken Shoemake, 1993)

	private enum Axis : int { X, Y, Z, W }

	private static int EulFrmS = 0;
	private static int EulFrmR = 1;

	private static int EulRepNo = 0;
	private static int EulRepYes = 1;

	private static int EulParEven = 0;
	private static int EulParOdd = 1;

	private static int[] EulSafe = new int[4] { 0, 1, 2, 0 };
	private static int[] EulNext = new int[4] { 1, 2, 0, 1 };
	private static void EulGetOrd(int ord, out int i, out int j, out int k, out int h, out int n, out int s, out int f) {
		var o = ord;
		f = o & 1; o >>= 1; s = o & 1; o >>= 1;
		n = o & 1; o >>= 1; i = EulSafe[o & 3]; j = EulNext[i + n]; k = EulNext[i + 1 - n]; h = s == 1 ? k : i;
	}
	private static int EulOrd(Axis i, int p, int r, int f) => ((((((((int)i) << 1) + (p)) << 1) + (r)) << 1) + (f));

	private static int Order = EulOrd(Axis.Z, EulParEven, EulRepNo, EulFrmS);

	internal static Vector3 MatrixToEuler(Matrix4x4 m) {
		Vector3 ea = default;

		var M = new float[,] {
			{ m.M11, m.M12, m.M13, m.M14 },
			{ m.M21, m.M22, m.M23, m.M24 },
			{ m.M31, m.M32, m.M33, m.M34 },
			{ m.M41, m.M42, m.M43, m.M44 }
		};

		EulGetOrd(Order, out var i, out var j, out var k, out var h, out var n, out var s, out var f);

		if (s == EulRepYes) {
			float sy = MathF.Sqrt(M[i, j] * M[i, j] + M[i, k] * M[i, k]);
			if (sy > 16 * float.Epsilon) {
				ea.X = MathF.Atan2(M[i, j], M[i, k]);
				ea.Y = MathF.Atan2(sy, M[i, i]);
				ea.Z = MathF.Atan2(M[j, i], -M[k, i]);
			} else {
				ea.X = MathF.Atan2(-M[j, k], M[j, j]);
				ea.Y = MathF.Atan2(sy, M[i, i]);
				ea.Z = 0;
			}
		} else {
			float cy = MathF.Sqrt(M[i, i] * M[i, i] + M[j, i] * M[j, i]);
			if (cy > 16 * float.Epsilon) {
				ea.X = MathF.Atan2(M[k, j], M[k, k]);
				ea.Y = MathF.Atan2(-M[k, i], cy);
				ea.Z = MathF.Atan2(M[j, i], M[i, i]);
			} else {
				ea.X = MathF.Atan2(-M[j, k], M[j, j]);
				ea.Y = MathF.Atan2(-M[k, i], cy);
				ea.Z = 0;
			}
		}
		if (n == EulParOdd) { ea.X = -ea.X; ea.Y = -ea.Y; ea.Z = -ea.Z; }
		if (f == EulFrmR) { float t = ea.X; ea.X = ea.Z; ea.Z = t; }

		return new Vector3(ea.Y, ea.Z, ea.X) * Rad2Deg;
	}

	internal static Matrix4x4 EulerToMatrix(Vector3 v) {
		var ea = new Vector3(v.Z, v.X, v.Y) * Deg2Rad;

		float ti, tj, th, ci, cj, ch, si, sj, sh, cc, cs, sc, ss;
		EulGetOrd(Order, out var i, out var j, out var k, out var h, out var n, out var s, out var f);

		if (f == EulFrmR) { var t = ea.X; ea.X = ea.Z; ea.Z = t; }
		if (n == EulParOdd) { ea.X = -ea.X; ea.Y = -ea.Y; ea.Z = -ea.Z; }

		float[,] M = new float[4, 4];

		ti = ea.X; tj = ea.Y; th = ea.Z;
		ci = MathF.Cos(ti); cj = MathF.Cos(tj); ch = MathF.Cos(th);
		si = MathF.Sin(ti); sj = MathF.Sin(tj); sh = MathF.Sin(th);
		cc = ci * ch; cs = ci * sh; sc = si * ch; ss = si * sh;
		if (s == EulRepYes) {
			M[i, i] = cj; M[i, j] = sj * si; M[i, k] = sj * ci;
			M[j, i] = sj * sh; M[j, j] = -cj * ss + cc; M[j, k] = -cj * cs - sc;
			M[k, i] = -sj * ch; M[k, j] = cj * sc + cs; M[k, k] = cj * cc - ss;
		} else {
			M[i, i] = cj * ch; M[i, j] = sj * sc - cs; M[i, k] = sj * cc + ss;
			M[j, i] = cj * sh; M[j, j] = sj * ss + cc; M[j, k] = sj * cs - sc;
			M[k, i] = -sj; M[k, j] = cj * si; M[k, k] = cj * ci;
		}

		return new Matrix4x4(
			M[0, 0], M[0, 1], M[0, 2], M[0, 3],
			M[1, 0], M[1, 1], M[1, 2], M[1, 3],
			M[2, 0], M[2, 1], M[2, 2], M[2, 3],
			M[3, 0], M[3, 1], M[3, 2], M[3, 3]
		);
	}

	internal static Vector3 ToEuler(Quaternion q) {
		float Nq = q.X * q.X + q.Y * q.Y + q.Z * q.Z + q.W * q.W;
		float s = (Nq > 0.0f) ? (2.0f / Nq) : 0.0f;
		float xs = q.X * s, ys = q.Y * s, zs = q.Z * s;
		float wx = q.W * xs, wy = q.W * ys, wz = q.W * zs;
		float xx = q.X * xs, xy = q.X * ys, xz = q.X * zs;
		float yy = q.Y * ys, yz = q.Y * zs, zz = q.Z * zs;

		var M = new Matrix4x4(
			1f - (yy + zz), xy - wz, xz + wy, 0,
			xy + wz, 1f - (xx + zz), yz - wx, 0,
			xz - wy, yz + wx, 1f - (xx + yy), 0,
			0f, 0f, 0f, 1f
		);
		
		return MatrixToEuler(M).NormalizeAngles();
	}

	public static Quaternion ToQuaternion(Vector3 v) {
		var ea = new Vector3(v.Z, v.X, v.Y) * Deg2Rad;

		Quaternion qu = default;
		var a = new float[3];
		float ti, tj, th, ci, cj, ch, si, sj, sh, cc, cs, sc, ss;
		EulGetOrd(Order, out var i, out var j, out var k, out var h, out var n, out var s, out var f);
		if (f == EulFrmR) { float t = ea.X; ea.X = ea.Z; ea.Z = t; }
		if (n == EulParOdd) ea.Y = -ea.Y;
		ti = ea.X * 0.5f; tj = ea.Y * 0.5f; th = ea.Z * 0.5f;
		ci = MathF.Cos(ti); cj = MathF.Cos(tj); ch = MathF.Cos(th);
		si = MathF.Sin(ti); sj = MathF.Sin(tj); sh = MathF.Sin(th);
		cc = ci * ch; cs = ci * sh; sc = si * ch; ss = si * sh;
		if (s == EulRepYes) {
			a[i] = cj * (cs + sc);
			a[j] = sj * (cc + ss);
			a[k] = sj * (cs - sc);
			qu.W = cj * (cc - ss);
		} else {
			a[i] = cj * sc - sj * cs;
			a[j] = cj * ss + sj * cc;
			a[k] = cj * cs - sj * sc;
			qu.W = cj * cc + sj * ss;
		}
		if (n == EulParOdd) a[j] = -a[j];
		qu.X = a[0]; qu.Y = a[1]; qu.Z = a[2];
		return qu;
	}
}
