using System;
using System.Numerics;

using Dalamud.Hooking;
using Dalamud.Logging;

using Ktisis.Overlay;

namespace Ktisis.Interop.Hooks {
	internal class CameraHooks {
		internal delegate IntPtr MakeProjectionMatrix2(IntPtr a1, float a2, float a3, float a4, float a5, float a6, float a7);
		internal static Hook<MakeProjectionMatrix2> ProjHook = null!;

		internal unsafe static IntPtr ProjMatrixDetour(IntPtr a1, float a2, float a3, float a4, float a5, float a6, float a7) {
			var exec = ProjHook.Original(a1, a2, a3, a4, a5, a6, a7);
			if (a5 >= 1000)
				OverlayWindow.ProjMatrix = *(Matrix4x4*)exec;
			return exec;
		}

		internal delegate IntPtr CalculateViewMatrix(IntPtr a1);
		internal static Hook<CalculateViewMatrix> ViewHook = null!;

		internal unsafe static IntPtr ViewMatrixDetour(IntPtr a1) {
			var exec = ViewHook.Original(a1);
			OverlayWindow.ViewMatrix = *(Matrix4x4*)exec;
			return exec;
		}

		internal static void Init() {
			var proj = Services.SigScanner.ScanText("E8 ?? ?? ?? ?? 4C 8B 2D ?? ?? ?? ?? 41 0F 28 C2");
			ProjHook = Hook<MakeProjectionMatrix2>.FromAddress(proj, ProjMatrixDetour);
			ProjHook.Enable();

			var view = Services.SigScanner.ScanText("E8 ?? ?? ?? ?? 33 C0 48 89 83 ?? ?? ?? ?? 48 8B 9C 24 ?? ?? ?? ??");
			ViewHook = Hook<CalculateViewMatrix>.FromAddress(view, ViewMatrixDetour);
			ViewHook.Enable();
		}

		internal static void Dispose() {
			ProjHook.Disable();
			ProjHook.Dispose();

			ViewHook.Disable();
			ViewHook.Dispose();
		}
	}
}