using System;

using Dalamud.Hooking;

namespace Ktisis.Interop.Hooks {
	internal class GuiHooks {
		// Target name in the GPose window

		internal delegate void TarNameDelegate(IntPtr a1);
		internal static Hook<TarNameDelegate> TarNameHook = null!;

		internal static string ReplaceTarName = "(Hidden by Ktisis)";
		internal unsafe static void UpdateTarName(IntPtr a1) {
			if (!Ktisis.Configuration.DisplayCharName) {
				for (var i = 0; i < ReplaceTarName.Length; i++)
					*(char*)(a1 + 488 + i) = ReplaceTarName[i];
			}
			TarNameHook.Original(a1);
		}

		// Init & dispose

		internal static void Init() {
			var tarName = Dalamud.SigScanner.ScanText("40 56 48 83 EC 50 48 8B 05 ?? ?? ?? ?? 48 8B F1 48 85 C0");
			TarNameHook = Hook<TarNameDelegate>.FromAddress(tarName, UpdateTarName);
			TarNameHook.Enable();
		}

		internal static void Dispose() {
			TarNameHook.Disable();
			TarNameHook.Dispose();
		}
	}
}