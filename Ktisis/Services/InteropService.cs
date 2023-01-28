using System;
using System.Numerics;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using Dalamud.Hooking;
using Dalamud.Logging;

using FFXIVClientStructs.Havok;

namespace Ktisis.Services {
	internal static class InteropService {
		// Allocations

		private static IntPtr MatrixAlloc;

		// Matrix Access

		internal unsafe static Matrix4x4* Matrix; // Align to 16-byte boundary
		internal unsafe static Matrix4x4 GetMatrix(hkQsTransformf* transform) {
			transform->get4x4ColumnMajor((float*)Matrix);
			return *Matrix;
		}
		internal unsafe static void SetMatrix(hkQsTransformf* transform, Matrix4x4 matrix) {
			*Matrix = matrix;
			transform->set((hkMatrix4f*)Matrix);
		}

		// Hooks

		private static List<Hook<Delegate>> Hooks = new();

		internal unsafe static void Init() {
			// Allocate space for our matrix to be aligned on a 16-byte boundary.
			// This is required due to ffxiv's use of the MOVAPS instruction.
			// Thanks to Fayti1703 for helping with debugging and coming up with this fix.
			MatrixAlloc = Marshal.AllocHGlobal(sizeof(float) * 16 + 16);
			Matrix = (Matrix4x4*)(16 * ((long)(MatrixAlloc + 15) / 16));

			PluginLog.Debug(string.Format(
				"Allocated matrix at {0:X}, aligned at {1:X}.",
				MatrixAlloc, (nint)Matrix
			));
		}

		internal static void Dispose() {
			Marshal.FreeHGlobal(MatrixAlloc);
		}
	}
}