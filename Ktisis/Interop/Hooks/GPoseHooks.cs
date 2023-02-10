using System;

using Dalamud.Hooking;

using Ktisis.Services;

namespace Ktisis.Interop.Hooks {
	internal class GPoseHooks {
		// Left click target

		internal delegate ushort LeftClickTarDelegate(nint a1, nint a2);
		internal static Hook<LeftClickTarDelegate> LeftClickTarHook = null!;

		internal static ushort LeftClickTarDetour(nint a1, nint a2) {
			if (GPoseService.IsInGPose && !Ktisis.Configuration.AllowTargetOnLeftClick)
				return 0;
			return LeftClickTarHook.Original(a1, a2);
		}

		// Right click target

		internal delegate nint RightClickTarDelegate(nint a1, nint a2);
		internal static Hook<RightClickTarDelegate> RightClickTarHook = null!;

		internal static nint RightClickTarDetour(nint a1, nint a2) {
			if (GPoseService.IsInGPose && !Ktisis.Configuration.AllowTargetOnRightClick)
				return 0;
			return RightClickTarHook.Original(a1, a2);
		}

		// Target name in the GPose window

		internal delegate void TarNameDelegate(IntPtr a1);
		internal static Hook<TarNameDelegate> TarNameHook = null!;

		internal static string ReplaceTarName = "(Hidden by Ktisis)";
		internal unsafe static void UpdateTarNameDetour(IntPtr a1) {
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
			var leftClick = DalamudServices.SigScanner.ScanText("E8 ?? ?? ?? ?? 48 8B BC 24 ?? ?? ?? ?? 48 8B 9C 24 ?? ?? ?? ?? 4C 8B BC 24 ?? ?? ?? ?? 41 C7 06 ?? ?? ?? ??");
			LeftClickTarHook = Hook<LeftClickTarDelegate>.FromAddress(leftClick, LeftClickTarDetour);
			LeftClickTarHook.Enable();

			var rightClick = DalamudServices.SigScanner.ScanText("E8 ?? ?? ?? ?? 48 8B D7 48 8B CB 48 8B 6C 24 ??");
			RightClickTarHook = Hook<RightClickTarDelegate>.FromAddress(rightClick, RightClickTarDetour);
			RightClickTarHook.Enable();

			var tarName = DalamudServices.SigScanner.ScanText("40 56 48 83 EC 50 48 8B 05 ?? ?? ?? ?? 48 8B F1 48 85 C0");
			TarNameHook = Hook<TarNameDelegate>.FromAddress(tarName, UpdateTarNameDetour);
			TarNameHook.Enable();
		}

		internal static void Dispose() {
			LeftClickTarHook.Disable();
			LeftClickTarHook.Dispose();

			RightClickTarHook.Disable();
			RightClickTarHook.Dispose();

			TarNameHook.Disable();
			TarNameHook.Dispose();
		}
	}
}
