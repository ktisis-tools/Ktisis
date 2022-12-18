using System;
using System.Numerics;

using Dalamud.Hooking;
using Dalamud.Game.ClientState.Keys;

using Ktisis.Scene;
using Ktisis.Events;
using Ktisis.Structs.Input;

namespace Ktisis.Interop.Hooks {
	internal class CameraHooks {
		// Delegates & Hooks

		internal unsafe delegate IntPtr MakeProjectionMatrix2(IntPtr ptr, float a2, float a3, float a4, float a5, float a6, float a7);
		internal static Hook<MakeProjectionMatrix2> ProjectionHook = null!;

		internal unsafe delegate Matrix4x4* CalculateViewMatrix(IntPtr a1);
		internal static Hook<CalculateViewMatrix> ViewHook = null!;

		// Init & Dispose

		internal static unsafe void Init() {
			var proj = Services.SigScanner.ScanText("E8 ?? ?? ?? ?? 4C 8B 2D ?? ?? ?? ?? 41 0F 28 C2 ");
			ProjectionHook = Hook<MakeProjectionMatrix2>.FromAddress(proj, ProjectionDetour);
			ProjectionHook.Enable();

			var view = Services.SigScanner.ScanText("E8 ?? ?? ?? ?? 33 C0 48 89 83 ?? ?? ?? ?? 48 8B 9C 24 ?? ?? ?? ??");
			ViewHook = Hook<CalculateViewMatrix>.FromAddress(view, ViewMatrixDetour);
			ViewHook.Enable();

			EventManager.OnKeyPressed += OnKeyPressed;
			EventManager.OnMouseEvent += OnMouseEvent;
		}

		internal static unsafe void Dispose() {
			ProjectionHook.Disable();
			ProjectionHook.Dispose();

			ViewHook.Disable();
			ViewHook.Dispose();
		}

		// Detours

		internal static IntPtr ProjectionDetour(IntPtr ptr, float a2, float a3, float a4, float a5, float a6, float a7) {
			var exec = ProjectionHook.Original(ptr, a2, a3, a4, a5, a6, a7);
			return exec;
		}

		internal static bool HasCaptured = false;
		internal static Matrix4x4 Matrix = new();

		internal const float MoveVel = 0.05f;

		internal unsafe static Matrix4x4* ViewMatrixDetour(IntPtr a1) {
			var exec = ViewHook.Original(a1);

			if (!WorkCamera.Active)
				return exec;

			var tar = ((IntPtr)Services.Camera->Camera) + 0x10;
			if (a1 == tar && exec != null && exec != (Matrix4x4*)IntPtr.Zero) {
				if (!HasCaptured) {
					HasCaptured = true;
					Matrix = *exec;

					WorkCamera.Position = *(Vector3*)(tar + 0x50);
				}

				*exec = WorkCamera.Update();
			}

			return exec;
		}

		internal unsafe static void OnMouseEvent(MouseState* state) {
			if (!WorkCamera.Active) return;
			if (!state->IsFocused || !state->Pressed.HasFlag(MouseButton.Right)) return;

			WorkCamera.MouseDelta += new Vector2(state->DeltaX, state->DeltaY);
		}

		internal static bool OnKeyPressed(QueueItem e) {
			if (!WorkCamera.Active)
				return false;

			switch (e.VirtualKey) {
				case VirtualKey.W:
				case VirtualKey.A:
				case VirtualKey.S:
				case VirtualKey.D:
				case VirtualKey.SPACE:
					return true;
			}

			return false;
		}

		internal static Matrix4x4 CreateViewMatrix(Vector3 pos, Vector3 dir, Vector3 up) {
			var len = (float)Math.Sqrt(dir.X * dir.X + dir.Y * dir.Y + dir.Z * dir.Z);
			var f = dir / len;

			var s = new Vector3(
				up.Y * f.Z - up.Z * f.Y,
				up.Z * f.X - up.X * f.Z,
				up.X * f.Y - up.Y * f.X
			);

			var len2 = (float)Math.Sqrt(s.X * s.X + s.Y * s.Y + s.Z * s.Z);
			var s_norm = s / len2;

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
	}
}