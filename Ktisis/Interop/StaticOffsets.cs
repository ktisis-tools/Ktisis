using Dalamud.Logging;

using System;

namespace Ktisis.Interop {
	internal static class StaticOffsets {
		// Address of loaded FFXIV_CHARA files in memory.
		internal static IntPtr CharaDatData;

		// If this is NOP'd, Anam posing is enabled.
		internal static IntPtr FreezePosition;
		internal static IntPtr FreezeRotation;
		internal static IntPtr FreezeScale;

		internal unsafe static bool IsPositionFrozen => FreezePosition != IntPtr.Zero && *(byte*)FreezePosition == 0x90;
		internal unsafe static bool IsRotationFrozen => FreezeRotation != IntPtr.Zero && *(byte*)FreezeRotation == 0x90;
		internal unsafe static bool IsScalingFrozen => FreezeScale != IntPtr.Zero && *(byte*)FreezeScale == 0x90;

		internal unsafe static bool IsAnamPosing => IsPositionFrozen || IsRotationFrozen || IsScalingFrozen;

		// Init
		internal unsafe static void Init() {
			var qword_14200E548 = *(IntPtr*)Services.SigScanner.GetStaticAddressFromSig("48 8B 05 ?? ?? ?? ?? 48 C7 44 24 24 05 00 00 00 C6 84 24");
			CharaDatData = *(IntPtr*)(qword_14200E548 + 1392);

			FreezePosition = Services.SigScanner.ScanText("41 0F 29 24 12");
			FreezeRotation = Services.SigScanner.ScanText("41 0F 29 5C 12 10");
			FreezeScale = Services.SigScanner.ScanText("41 0F 29 44 12 20");
		}
	}
}