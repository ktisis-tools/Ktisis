using System;
using System.Numerics;

using Dalamud.Hooking;

using Ktisis.Overlay;

namespace Ktisis.Interop.Hooks {
	internal class CameraHooks {
		internal delegate IntPtr CalculateViewMatrix(IntPtr a1);
		internal static Hook<CalculateViewMatrix> ViewHook = null!;

		internal unsafe static IntPtr ViewMatrixDetour(IntPtr a1) {
			var exec = ViewHook.Original(a1);
			OverlayWindow.ViewMatrix = *(Matrix4x4*)exec;
			return exec;
		}

		internal static void Init() {
			var view = Services.SigScanner.ScanText("E8 ?? ?? ?? ?? 33 C0 48 89 83 ?? ?? ?? ?? 48 8B 9C 24 ?? ?? ?? ??");
			ViewHook = Hook<CalculateViewMatrix>.FromAddress(view, ViewMatrixDetour);
			ViewHook.Enable();
		}

		internal static void Dispose() {
			ViewHook.Disable();
			ViewHook.Dispose();
		}
	}
}