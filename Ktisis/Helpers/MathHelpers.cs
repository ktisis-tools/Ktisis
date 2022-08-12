using System;
using System.Numerics;

namespace Ktisis.Helpers {
    public class MathHelpers {
        // Normalize Vector4

        public static Vector3 ToVector3(Vector4 v) {
            return new Vector3(v.X, v.Y, v.Z);
        }

        // Degreees <=> Radians

        private static readonly float Deg2Rad = ((float)Math.PI * 2) / 360;
        private static readonly float Rad2Deg = 360 / ((float)Math.PI * 2);

        // Euler <=> Quaternion
        // Borrowed from BDTH

        public static Quaternion ToQuaternion(Vector3 euler) {
            var xOver2 = euler.X * Deg2Rad * 0.5f;
            var yOver2 = euler.Y * Deg2Rad * 0.5f;
            var zOver2 = euler.Z * Deg2Rad * 0.5f;

            var sinXOver2 = (float)Math.Sin(xOver2);
            var cosXOver2 = (float)Math.Cos(xOver2);
            var sinYOver2 = (float)Math.Sin(yOver2);
            var cosYOver2 = (float)Math.Cos(yOver2);
            var sinZOver2 = (float)Math.Sin(zOver2);
            var cosZOver2 = (float)Math.Cos(zOver2);

            Quaternion result;
            result.X = cosYOver2 * sinXOver2 * cosZOver2 + sinYOver2 * cosXOver2 * sinZOver2;
            result.Y = sinYOver2 * cosXOver2 * cosZOver2 - cosYOver2 * sinXOver2 * sinZOver2;
            result.Z = cosYOver2 * cosXOver2 * sinZOver2 - sinYOver2 * sinXOver2 * cosZOver2;
            result.W = cosYOver2 * cosXOver2 * cosZOver2 + sinYOver2 * sinXOver2 * sinZOver2;
            return result;
        }

        public static Vector3 ToEuler(Quaternion q2) {
            Quaternion q = new Quaternion(q2.W, q2.Z, q2.X, q2.Y);
            Vector3 pitchYawRoll = new Vector3 {
                Y = (float)Math.Atan2(2f * q.X * q.W + 2f * q.Y * q.Z, 1 - 2f * (q.Z * q.Z + q.W * q.W)),
                X = (float)Math.Asin(2f * (q.X * q.Z - q.W * q.Y)),
                Z = (float)Math.Atan2(2f * q.X * q.Y + 2f * q.Z * q.W, 1 - 2f * (q.Y * q.Y + q.Z * q.Z))
            };

            pitchYawRoll.X *= Rad2Deg;
            pitchYawRoll.Y *= Rad2Deg;
            pitchYawRoll.Z *= Rad2Deg;

            return pitchYawRoll;
        }
    }
}