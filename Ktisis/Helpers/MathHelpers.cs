using System;
using System.Numerics;

namespace Ktisis.Helpers {
	public class MathHelpers {
		public static Vector3 Normalize(Vector4 v) {
			return new Vector3(v.X / v.W, v.Y / v.W, v.Z / v.W);
		}

		public static Vector3 ToEuler(Quaternion q) {
			Vector3 angles = new Vector3();

			double sinr_cosp = 2 * (q.W * q.X + q.Y * q.Z);
			double cosr_cosp = 1 - 2 * (q.X * q.X + q.Y * q.Y);
            angles.X = (float)Math.Atan2(sinr_cosp, cosr_cosp);

			double sinp = 2 * (q.W * q.Y - q.Z * q.X);
			angles.Y = Math.Abs(sinp) >= 1
				? (float)Math.CopySign(Math.PI / 2, sinp)
				: (float)Math.Asin(sinp);

			double siny_cosp = 2 * (q.W * q.Z + q.X * q.Y);
			double cosy_cosp = 1 - 2 * (q.Y * q.Y + q.Z * q.Z);
			angles.Z = (float)Math.Atan2(siny_cosp, cosy_cosp);

			return angles;
		}
    }
}