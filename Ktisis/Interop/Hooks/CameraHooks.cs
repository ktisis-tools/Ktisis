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

			if (!WorkCamera.Active)
				return exec;

			// TODO

			return exec;
		}

		internal unsafe static Matrix4x4* ViewMatrixDetour(IntPtr a1) {
			var exec = ViewHook.Original(a1);

			if (!WorkCamera.Active)
				return exec;

			var tar = ((IntPtr)Services.Camera->Camera) + 0x10;
			if (a1 == tar && exec != null && exec != (Matrix4x4*)IntPtr.Zero)
				*exec = WorkCamera.Update();

			return exec;
		}

		internal unsafe static void OnMouseEvent(MouseState* state) {
			if (!WorkCamera.Active) return;
			if (!state->IsFocused || !state->Pressed.HasFlag(MouseButton.Right)) return;

			WorkCamera.MouseDelta += new Vector2(state->DeltaX, state->DeltaY);
		}

		internal static bool OnKeyPressed(QueueItem e) {
			if (WorkCamera.Active) {
				switch (e.VirtualKey) {
					case VirtualKey.W:
					case VirtualKey.A:
					case VirtualKey.S:
					case VirtualKey.D:
					case VirtualKey.SPACE:
						return true;
				}
			}

			return false;
		}
	}
}