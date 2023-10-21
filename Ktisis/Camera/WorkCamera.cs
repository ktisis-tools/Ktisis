using System;
using System.Numerics;

using GameCamera = FFXIVClientStructs.FFXIV.Client.Game.Camera;
using SceneCamera = FFXIVClientStructs.FFXIV.Client.Graphics.Scene.Camera;

using Ktisis.Helpers;
using Ktisis.Interface;
using Ktisis.Structs.Input;

namespace Ktisis.Camera {
	public class WorkCamera {
		public bool Active;

		public Vector3 Position;
		public Vector3 Rotation = new(0f, 0f, 0f);
		private Vector3 UpVector = new(0f, 1f, 0f);

		private const float ClampY = 1.57072f;
		private static float DefaultSpeed => Ktisis.Configuration.FreecamMoveSpeed;
		private float MoveSpeed = DefaultSpeed;

		private Vector3 Velocity;
		private Vector2 MouseDelta;
		
		internal Vector3 InterpPos;

		private DateTime LastTime;
		
		public void SetActive(bool active) {
			Active = active;
			if (Active) {
				LastTime = DateTime.Now;
				InterpPos = Position;
			}
		}

		internal Matrix4x4 Update(float fov = 1) {
			var now = DateTime.Now;
			var delta = Math.Max((float)(now - LastTime).TotalMilliseconds, 1);
			LastTime = now;

			MouseDelta = (MouseDelta * fov * Ktisis.Configuration.FreecamSensitivity) * MathHelpers.Deg2Rad;
			Rotation.X -= MouseDelta.X;
			Rotation.Y = Math.Max(Math.Min(Rotation.Y + MouseDelta.Y, ClampY), -ClampY);
			MouseDelta = Vector2.Zero;

			Position += Velocity * MoveSpeed * fov;
			InterpPos = Vector3.Lerp(InterpPos, Position, 8f / delta);

			return CreateViewMatrix();
		}

		internal unsafe void UpdateControl(MouseState* mouseState, KeyboardState* keyState) {
			bool rightHeld = false;
			if (mouseState != null) {
				var mouseDelta = mouseState->GetDelta();
				rightHeld = mouseState->IsButtonHeld(MouseButton.Right);
				if (rightHeld)
					MouseDelta += mouseDelta;
			}

			MoveSpeed = DefaultSpeed;
			if (keyState != null && !Input.IsChatInputActive()) {
				if (Ktisis.Configuration.FreecamFast.IsActive(keyState))
					MoveSpeed *= Ktisis.Configuration.FreecamShiftMuli;
				else if (Ktisis.Configuration.FreecamSlow.IsActive(keyState))
					MoveSpeed *= Ktisis.Configuration.FreecamCtrlMuli;
				
				var vFwb = 0;
				var bothHeld = rightHeld && mouseState->IsButtonHeld(MouseButton.Left);
				if (Ktisis.Configuration.FreecamForward.IsActive(keyState) || bothHeld) vFwb -= 1; // Forward
				if (Ktisis.Configuration.FreecamBack.IsActive(keyState)) vFwb += 1; // Back
				
				var vLr = 0;
				if (Ktisis.Configuration.FreecamLeft.IsActive(keyState)) vLr -= 1; // Left
				if (Ktisis.Configuration.FreecamRight.IsActive(keyState)) vLr += 1; // Right

				Velocity.X = vFwb * (float)Math.Sin(Rotation.X) * (float)Math.Cos(Rotation.Y) + (vLr * (float)Math.Cos(Rotation.X));
				Velocity.Y = vFwb * (float)Math.Sin(Rotation.Y);
				Velocity.Z = vFwb * (float)Math.Cos(Rotation.X) * (float)Math.Cos(Rotation.Y) + (-vLr * (float)Math.Sin(Rotation.X));
				
				if (Ktisis.Configuration.FreecamUp.IsActive(keyState))
					Velocity.Y += Ktisis.Configuration.FreecamUpDownMuli;
				else if (Ktisis.Configuration.FreecamDown.IsActive(keyState))
					Velocity.Y -= Ktisis.Configuration.FreecamUpDownMuli;
			}
		}

		private Matrix4x4 CreateViewMatrix() {
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

		internal Vector3 GetLookDir() => new(
			(float)Math.Sin(Rotation.X) * (float)Math.Cos(Rotation.Y),
			(float)Math.Sin(Rotation.Y),
			(float)Math.Cos(Rotation.X) * (float)Math.Cos(Rotation.Y)
		);
	}
}