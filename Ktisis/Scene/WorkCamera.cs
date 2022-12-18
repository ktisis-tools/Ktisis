using System;
using System.Numerics;

using Dalamud.Game.ClientState.Keys;

using Ktisis.Events;
using Ktisis.Interface;

namespace Ktisis.Scene {
	public static class WorkCamera {
		private static bool _Active = false;
		public static bool Active => _Active && Ktisis.IsInGPose;
		public static void Toggle() {
			_Active = !_Active;
			if (Active) {
				// TODO: ClientStructs PR
				unsafe {
					Position = *(Vector3*)((IntPtr)Services.Camera->Camera + 0x60);
				}
			}
		}

		public static Vector3 Position = new();
		public static Vector3 Rotation = new(0f, 0f, 1f);
		public static Vector3 UpVector = new(0f, 2f, 0f);

		public static float MoveSpeed = 0.005f;

		internal static Vector3 InterpPos = new();
		internal static Vector2 MouseDelta = new();

		private static DateTime LastTime = DateTime.Now;

		internal static Matrix4x4 Update() {
			var now = DateTime.Now;
			var delta = (now - LastTime).Milliseconds;
			LastTime = now;

			// Movement

			var vel = MoveSpeed * delta;
			if (EventManager.IsKeyDown(VirtualKey.SHIFT))
				vel *= 5f;
			else if (EventManager.IsKeyDown(VirtualKey.CONTROL))
				vel /= 5f;

			var newPos = Position;
			if (Input.IsPurposeUsed(Input.Purpose.CameraForward)) { // Forward
				newPos += new Vector3(
					-vel * ((float)Math.Sin(Rotation.X) * (float)Math.Cos(Rotation.Y)),
					-vel * (float)Math.Sin(Rotation.Y),
					-vel * (float)Math.Cos(Rotation.X) * (float)Math.Cos(Rotation.Y)
				);
			}
			if (Input.IsPurposeUsed(Input.Purpose.CameraLeft)) { // Left
				newPos += new Vector3(
					-vel * (float)Math.Cos(Rotation.X),
					0,
					vel * (float)Math.Sin(Rotation.X)
				);
			}
			if (Input.IsPurposeUsed(Input.Purpose.CameraBackward)) { // Back
				newPos += new Vector3(
					vel * (float)Math.Sin(Rotation.X) * (float)Math.Cos(Rotation.Y),
					vel * (float)Math.Sin(Rotation.Y),
					vel * (float)Math.Cos(Rotation.X) * (float)Math.Cos(Rotation.Y)
				);
			}
			if (Input.IsPurposeUsed(Input.Purpose.CameraRight)) { // Right
				newPos += new Vector3(
					vel * (float)Math.Cos(Rotation.X),
					0,
					-vel * (float)Math.Sin(Rotation.X)
				);
			}
			if (Input.IsPurposeUsed(Input.Purpose.CameraUp)) {
				newPos.Y += vel / 1.25f;
			}

			Position = newPos;
			InterpPos = Lerp(0.5f);

			// Rotation

			var mouseDelta = (MouseDelta / 100f) * (float)(Math.PI / 180) * 7.5f;
			MouseDelta.X = 0;
			MouseDelta.Y = 0;

			Rotation.X -= mouseDelta.X;
			Rotation.Y = Math.Max(Math.Min(Rotation.Y + mouseDelta.Y, 1.57075f), -1.57075f);
			Rotation.Z = Rotation.X;

			// Create view matrix

			return CreateViewMatrix();
		}

		internal static Matrix4x4 CreateViewMatrix() {
			var pos = InterpPos;
			var dir = GetLookDir();
			var up = UpVector;

			var f_len = (float)Math.Sqrt(dir.X * dir.X + dir.Y * dir.Y + dir.Z * dir.Z);
			var f = dir / f_len;

			var s = new Vector3(
				up.Y * f.Z - up.Z * f.Y,
				up.Z * f.X - up.X * f.Z,
				up.X * f.Y - up.Y * f.X
			);

			var s_len = (float)Math.Sqrt(s.X * s.X + s.Y * s.Y + s.Z * s.Z);
			var s_norm = s / s_len;

			var u = new Vector3(
				f.Y * s_norm.Z - f.Z * s_norm.Y,
				f.Z * s_norm.X - f.X * s_norm.Z,
				f.X * s_norm.Y - f.Y * s_norm.X
			);

			var p = new Vector3(
				-pos.X * s_norm.X - pos.Y * s_norm.Y - pos.Z * s_norm.Z,
				-pos.X * u.X - pos.Y * u.Y - pos.Z * u.Z,
				-pos.X * f.X - pos.Y * f.Y - pos.Z * f.Z
			);

			return new Matrix4x4(
				s_norm.X, u.X, f.X, 0.0f,
				s_norm.Y, u.Y, f.Y, 0.0f,
				s_norm.Z, u.Z, f.Z, 0.0f,
				p.X, p.Y, p.Z, 1.0f
			);
		}

		internal static Vector3 Lerp(float t)
			=> InterpPos + (Position - InterpPos) * t;

		private static Vector3 GetLookDir() => new Vector3(
			(float)Math.Sin(Rotation.X) * (float)Math.Cos(Rotation.Y),
			(float)Math.Sin(Rotation.Y),
			(float)Math.Cos(Rotation.Z) * (float)Math.Cos(Rotation.Y)
		);
	}
}