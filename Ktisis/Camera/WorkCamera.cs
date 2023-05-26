using System;
using System.Numerics;

using Dalamud.Game.ClientState.Keys;

using SceneCamera = FFXIVClientStructs.FFXIV.Client.Graphics.Scene.Camera;

using Ktisis.Structs.Input;

namespace Ktisis.Camera {
	public static class WorkCamera {
		public static bool Active;

		private static Vector3 Position;
		private static Vector3 Rotation = new(0f, 0f, 1f);
		private static Vector3 UpVector = new(0f, 2f, 0f);

		private const float DefaultSpeed = 0.005f;
		private static float MoveSpeed = DefaultSpeed;

		private static Vector3 Velocity;
		private static Vector3 InterpPos;
		private static Vector2 MouseDelta;

		private static DateTime LastTime;
		
		public unsafe static void Toggle() {
			Active = !Active;
			if (Active) {
				LastTime = DateTime.Now;
				Position = Services.Camera->Camera->CameraBase.SceneCamera.Object.Position;
			}
		}

		internal static Matrix4x4 Update(float fov = 1) {
			var now = DateTime.Now;
			var delta = (float)(now - LastTime).TotalMilliseconds;
			LastTime = now;
			
			MouseDelta *= delta * fov;
			Rotation.X -= MouseDelta.X;
			Rotation.Y = Math.Max(Math.Min(Rotation.Y + MouseDelta.Y, 1.57075f), -1.57075f);
			Rotation.Z = Rotation.X;
			MouseDelta = Vector2.Zero;
			
			Position += Velocity * MoveSpeed * fov * delta;
			InterpPos = Position + (Position - InterpPos) * 0.05f;

			return CreateViewMatrix();
		}

		internal unsafe static void UpdateControl(MouseState* mouseState, KeyboardState* keyState) {
			if (mouseState != null && mouseState->IsButtonHeld(MouseButton.Right)) {
				MouseDelta.X += ConvertDelta(mouseState->DeltaX);
				MouseDelta.Y += ConvertDelta(mouseState->DeltaY);
			}

			MoveSpeed = DefaultSpeed;
			if (keyState != null) {
				if (keyState->IsKeyDown(VirtualKey.SHIFT, true))
					MoveSpeed *= 5f;
				else if (keyState->IsKeyDown(VirtualKey.CONTROL, true))
					MoveSpeed /= 5f;
				
				var vFwb = 0;
				if (keyState->IsKeyDown(VirtualKey.W, true)) vFwb -= 1; // Forward
				if (keyState->IsKeyDown(VirtualKey.S, true)) vFwb += 1; // Back
				
				var vLr = 0;
				if (keyState->IsKeyDown(VirtualKey.A, true)) vLr -= 1; // Left
				if (keyState->IsKeyDown(VirtualKey.D, true)) vLr += 1; // Right

				Velocity.X = vFwb * (float)Math.Sin(Rotation.X) * (float)Math.Cos(Rotation.Y) + (vLr * (float)Math.Cos(Rotation.X));
				Velocity.Y = vFwb * (float)Math.Sin(Rotation.Y);
				Velocity.Z = vFwb * (float)Math.Cos(Rotation.X) * (float)Math.Cos(Rotation.Y) + (-vLr * (float)Math.Sin(Rotation.X));
				
				if (keyState->IsKeyDown(VirtualKey.SPACE, true)) Velocity.Y += 0.75f;
			}
		}

		private static Matrix4x4 CreateViewMatrix() {
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

		private static float ConvertDelta(float delta) => (delta / 100f) * (float)(Math.PI / 180);

		private static Vector3 GetLookDir() => new(
			(float)Math.Sin(Rotation.X) * (float)Math.Cos(Rotation.Y),
			(float)Math.Sin(Rotation.Y),
			(float)Math.Cos(Rotation.Z) * (float)Math.Cos(Rotation.Y)
		);
	}
}