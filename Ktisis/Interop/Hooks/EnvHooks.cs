using Dalamud.Hooking;

using Ktisis.Env;
using Ktisis.Structs.Env;

namespace Ktisis.Interop.Hooks {
	public static class EnvHooks {
		// Hooks

		private unsafe delegate nint EnvUpdateDelegate(EnvManagerEx* env, nint a2);
		private delegate nint SkyTexDelegate(nint a1, nint a2);

		private static Hook<EnvUpdateDelegate> EnvUpdateHook = null!;
		private unsafe static nint EnvUpdateDetour(EnvManagerEx* env, nint a2) {
			Ktisis.Log.Info($"{(nint)env:X} {Ktisis.IsInGPose} {EnvService.TimeOverride}");
			if (Ktisis.IsInGPose && EnvService.TimeOverride != null) {
				Ktisis.Log.Info($"{env->Time}");
				env->Time = EnvService.TimeOverride.Value;
			}

			return EnvUpdateHook.Original(env, a2);
		}

		private static Hook<SkyTexDelegate> SkyTexHook = null!;
		private unsafe static nint SkyTexDetour(nint a1, nint a2) {
			var res = SkyTexHook.Original(a1, a2);
			
			if (Ktisis.IsInGPose && EnvService.SkyOverride != null)
				*(uint*)(a1 + 8) = EnvService.SkyOverride.Value;
			
			return res;
		}

		private delegate nint WaterRendererUpdateDelegate(nint a1);
		private static Hook<WaterRendererUpdateDelegate> WaterRendererUpdateHook = null!;
		private static nint WaterRendererUpdateDetour(nint a1) {
			if (Ktisis.IsInGPose && EnvService.FreezeWater == true) {
				return 0;
			}
			return WaterRendererUpdateHook.Original(a1);
		}
		
		
		// State

		private static bool Enabled;
		
		internal static void SetEnabled(bool enable) {
			if (Enabled == enable) return;
			if (enable)
				EnableHooks();
			else
				DisableHooks();
		}

		private static void EnableHooks() {
			Enabled = true;
			EnvUpdateHook.Enable();
			SkyTexHook.Enable();
			WaterRendererUpdateHook.Enable();
		}
		
		private static void DisableHooks() {
			Enabled = false;
			EnvUpdateHook.Disable();
			SkyTexHook.Disable();
			WaterRendererUpdateHook.Disable();
		}
		
		// Init & Dispose
		
		public unsafe static void Init() {
			var addr1 = Services.SigScanner.ScanText("40 53 48 83 EC 30 48 8B 05 ?? ?? ?? ?? 48 8B D9 0F 29 74 24 ??");
            EnvUpdateHook = Services.Hooking.HookFromAddress<EnvUpdateDelegate>(addr1, EnvUpdateDetour);
            
			var addr2 = Services.SigScanner.ScanText("E8 ?? ?? ?? ?? 49 3B F5 75 0D");
            SkyTexHook = Services.Hooking.HookFromAddress<SkyTexDelegate>(addr2, SkyTexDetour);
			
			var addr3 = Services.SigScanner.ScanText("48 8B C4 48 89 58 18 57 48 81 EC ?? ?? ?? ?? 0F B6 B9 ?? ?? ?? ??");
			WaterRendererUpdateHook = Services.Hooking.HookFromAddress<WaterRendererUpdateDelegate>(addr3, WaterRendererUpdateDetour);
        }

		public static void Dispose() {
			DisableHooks();
			
			EnvUpdateHook.Dispose();
			EnvUpdateHook.Dispose();
			
			SkyTexHook.Disable();
			SkyTexHook.Dispose();
			
			WaterRendererUpdateHook.Disable();
			WaterRendererUpdateHook.Dispose();
		}
	}
}
