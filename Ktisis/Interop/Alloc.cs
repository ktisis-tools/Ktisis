using System;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Ktisis.Interop {
	internal static class Alloc {
		// Allocations
		private static IntPtr MatrixAlloc;

		// Access
		internal unsafe static Matrix4x4 GetMatrix() => *(Matrix4x4*)(16 * ((long)(MatrixAlloc + 15) / 16)); // Align to 16-byte boundary

		// Init & disspose
		public static void Init() {
			// Allocate space for our matrix to be aligned on a 16-byte boundary.
			// This is required due to ffxiv's use of the MOVAPS instruction.
			// Thanks to Fayti1703 for helping with debugging and coming up with this fix.
			MatrixAlloc = Marshal.AllocHGlobal(sizeof(float) * 16 + 16);
		}
		public static void Dispose() {
			Marshal.FreeHGlobal(MatrixAlloc);
		}
	}
}