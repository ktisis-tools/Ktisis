using System;
using System.Numerics;

namespace Ktisis.Helpers {
	public static class MathHelpers {
		// Normalize Vector4

		public static Vector3 ToVector3(Vector4 v) {
			return new Vector3(v.X, v.Y, v.Z);
		}

		// Degreees <=> Radians

		public static readonly float Deg2Rad = ((float)Math.PI * 2) / 360;
		public static readonly float Rad2Deg = 360 / ((float)Math.PI * 2);

		// Euler <=> Quaternion
		// Borrowed from Anamnesis

		public static Quaternion ToQuaternion(Vector3 euler) {
			double yaw = euler.Y * Deg2Rad;
			double pitch = euler.X * Deg2Rad;
			double roll = euler.Z * Deg2Rad;

			double c1 = Math.Cos(yaw / 2);
			double s1 = Math.Sin(yaw / 2);
			double c2 = Math.Cos(pitch / 2);
			double s2 = Math.Sin(pitch / 2);
			double c3 = Math.Cos(roll / 2);
			double s3 = Math.Sin(roll / 2);

			double c1c2 = c1 * c2;
			double s1s2 = s1 * s2;

			double x = (c1c2 * s3) + (s1s2 * c3);
			double y = (s1 * c2 * c3) + (c1 * s2 * s3);
			double z = (c1 * s2 * c3) - (s1 * c2 * s3);
			double w = (c1c2 * c3) - (s1s2 * s3);

			return new Quaternion((float)x, (float)y, (float)z, (float)w);
		}

		public static Vector3 ToEuler(Quaternion q) {
			Vector3 v = new();

			double test = (q.X * q.Y) + (q.Z * q.W);

			if (test > 0.4995f) {
				v.Y = 2f * (float)Math.Atan2(q.X, q.Y);
				v.X = (float)Math.PI / 2;
				v.Z = 0;
			} else if (test < -0.4995f) {
				v.Y = -2f * (float)Math.Atan2(q.X, q.W);
				v.X = -(float)Math.PI / 2;
				v.Z = 0;
			} else {
				double sqx = q.X * q.X;
				double sqy = q.Y * q.Y;
				double sqz = q.Z * q.Z;

				v.Y = (float)Math.Atan2((2 * q.Y * q.W) - (2 * q.X * q.Z), 1 - (2 * sqy) - (2 * sqz));
				v.X = (float)Math.Asin(2 * test);
				v.Z = (float)Math.Atan2((2 * q.X * q.W) - (2 * q.Y * q.Z), 1 - (2 * sqx) - (2 * sqz));
			}

			v *= Rad2Deg;
			return NormalizeAngles(v);
		}

		public static Vector3 ToRadians(Vector3 vec) {
			vec.X *= Deg2Rad;
			vec.Y *= Deg2Rad;
			vec.Z *= Deg2Rad;
			return vec;
		}

		public static Vector3 ToEuler2(Quaternion q2) {
			var q = new Quaternion(q2.W, q2.Z, q2.X, q2.Y);
			return ToEuler(q);
		}

		public static Vector3 NormalizeAngles(Vector3 v) {
			v.X = NormalizeAngle(v.X);
			v.Y = NormalizeAngle(v.Y);
			v.Z = NormalizeAngle(v.Z);
			return v;
		}

		public static float NormalizeAngle(float angle) {
			while (angle > 180)
				angle -= 360;

			while (angle < -180)
				angle += 360;

			return angle;
		}

	}
}
