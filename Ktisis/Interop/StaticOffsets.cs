using System;

namespace Ktisis.Interop {
	internal class StaticOffsets {
		// Address of loaded FFXIV_CHARA files in memory.
		internal static IntPtr CharaDatData;

		// Init
		internal unsafe static void Init() {
			var qword_14200E548 = *(IntPtr*)Services.SigScanner.GetStaticAddressFromSig("48 8B 05 ?? ?? ?? ?? 48 C7 44 24 24 05 00 00 00 C6 84 24");
			CharaDatData = *(IntPtr*)(qword_14200E548 + 1392);
		}
	}
}