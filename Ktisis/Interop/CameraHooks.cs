using System;
using System.Runtime.InteropServices;

namespace Ktisis.Interop {
	internal class CameraHooks {
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal delegate IntPtr GetMatrixDelegate();
		internal static GetMatrixDelegate? GetMatrix;

		internal static void Init() {
			var matrixAddr = Dalamud.SigScanner.ScanText("E8 ?? ?? ?? ?? 48 8D 4C 24 ?? 48 89 4c 24 ?? 4C 8D 4D ?? 4C 8D 44 24 ??");
			GetMatrix = Marshal.GetDelegateForFunctionPointer<GetMatrixDelegate>(matrixAddr);
		}

		internal static void Dispose() {
			GetMatrix = null;
		}
	}
}