using System;

using Dalamud.Hooking;

using Ktisis.Services;

namespace Ktisis.Interop.Hooks {
	internal class GuiHooks {
		// Target name in the GPose window

		internal delegate void TarNameDelegate(IntPtr a1);
		internal static Hook<TarNameDelegate> TarNameHook = null!;

		internal static string ReplaceTarName = "(Hidden by Ktisis)";
		internal unsafe static void UpdateTarName(IntPtr a1) {
			try {
				string nameToDisplay = ReplaceTarName;

				if (Ktisis.Configuration.DisplayCharName) {
					var actor = GPoseService.TargetActor;
					if (actor != null && actor->Model != null && actor->Name != null)
							nameToDisplay = actor->Name!;
				}

				for (var i = 0; i < nameToDisplay.Length; i++)
					*(char*)(a1 + 488 + i) = nameToDisplay[i];
			} catch (Exception e) {
				Logger.Error(e, "Error in UpdateTarName. Disabling hook.");
				TarNameHook.Disable();
			}

			TarNameHook.Original(a1);
		}

		// Init & dispose

		internal static void Init() {
			var tarName = DalamudServices.SigScanner.ScanText("40 56 48 83 EC 50 48 8B 05 ?? ?? ?? ?? 48 8B F1 48 85 C0");
			TarNameHook = Hook<TarNameDelegate>.FromAddress(tarName, UpdateTarName);
			TarNameHook.Enable();
		}

		internal static void Dispose() {
			TarNameHook.Disable();
			TarNameHook.Dispose();
		}
	}
}
