using System;
using System.Numerics;
using System.Runtime.InteropServices;

using FFXIVClientStructs.FFXIV.Client.System.Memory;
using FFXIVClientStructs.Havok.Common.Base.Math.Matrix;
using FFXIVClientStructs.Havok.Common.Base.Math.QsTransform;

namespace Ktisis.Interop {
	internal static class Alloc {
		// Allocations
		private static IntPtr MatrixAlloc;

		// Access
		internal unsafe static Matrix4x4* Matrix; // Align to 16-byte boundary
		internal unsafe static Matrix4x4 GetMatrix(hkQsTransformf* transform) {
			transform->get4x4ColumnMajor((float*)Matrix);
			return *Matrix;
		}
		internal unsafe static void SetMatrix(hkQsTransformf* transform, Matrix4x4 matrix) {
			*Matrix = matrix;
			transform->set((hkMatrix4f*)Matrix);
		}

		// Init & disspose
		public unsafe static void Init() {
			// Allocate space for our matrix to be aligned on a 16-byte boundary.
			// This is required due to ffxiv's use of the MOVAPS instruction.
			// Thanks to Fayti1703 for helping with debugging and coming up with this fix.
			MatrixAlloc = Marshal.AllocHGlobal(sizeof(float) * 16 + 16);
			Matrix = (Matrix4x4*)(16 * ((long)(MatrixAlloc + 15) / 16));
		}
		public static void Dispose() {
			Marshal.FreeHGlobal(MatrixAlloc);
		}
	}
	
	internal class GameAlloc<T> : IDisposable where T : unmanaged {
		private bool Disposed;

		internal readonly nint Address;
		internal unsafe T* Data => (T*)Address;
	
		internal unsafe GameAlloc(ulong align = 16)
			=> Address = (nint)IMemorySpace.GetDefaultSpace()->Malloc<T>(align);

		public unsafe void Dispose() {
			if (Disposed) return;
			IMemorySpace.Free(Data); // Free our allocated memory.
			Disposed = true;
		}
	}
}