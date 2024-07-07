namespace Ktisis.Interop {
	internal static class StaticOffsets {
		// If this is NOP'd, Anam posing is enabled.
		internal unsafe static byte* FreezePosition;
		internal unsafe static byte* FreezeRotation;
		internal unsafe static byte* FreezeScale;

		internal unsafe static bool IsPositionFrozen => FreezePosition != null && *FreezePosition == 0x90 || *FreezePosition == 0x00;
		internal unsafe static bool IsRotationFrozen => FreezeRotation != null && *FreezeRotation == 0x90 || *FreezeRotation == 0x00;
		internal unsafe static bool IsScalingFrozen => FreezeScale != null && *FreezeScale == 0x90 || *FreezeScale == 0x00;

		internal static bool IsAnamPosing => IsPositionFrozen || IsRotationFrozen || IsScalingFrozen;

		// Init
		internal unsafe static void Init() {
			FreezePosition = (byte*)Services.SigScanner.ScanText("41 0F 29 24 12");
			FreezeRotation = (byte*)Services.SigScanner.ScanText("41 0F 29 5C 12 10");
			FreezeScale = (byte*)Services.SigScanner.ScanText("41 0F 29 44 12 20");
		}
	}
}
