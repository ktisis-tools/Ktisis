using System;

using Dalamud.Hooking;

using Ktisis.Events;
using Ktisis.Structs.Actor.State;
using Ktisis.Structs.FFXIV;

namespace Ktisis.Interop.Hooks {
	internal static class WorldHooks {
		public static bool TimeUpdateDisabled = false;
		public static bool WeatherUpdateDisabled = false;


		internal delegate void UpdateEorzeaTimeDelegate(IntPtr a1, IntPtr a2);
		internal static Hook<UpdateEorzeaTimeDelegate> UpdateEorzeaTimeHook = null!;

		internal delegate void UpdateTerritoryWeatherDelegate(IntPtr a1, IntPtr a2);
		internal static Hook<UpdateTerritoryWeatherDelegate> UpdateTerritoryWeatherHook = null!;

		public static unsafe WeatherSystem* WeatherSystem;

		internal unsafe static void UpdateEorzeaTime(IntPtr a1, IntPtr a2) {
			if (TimeUpdateDisabled)
				return;

			UpdateEorzeaTimeHook.Original(a1, a2);
		}

		internal unsafe static void UpdateTerritoryWeather(IntPtr a1, IntPtr a2) {
			if (WeatherUpdateDisabled)
				return;

			UpdateTerritoryWeatherHook.Original(a1, a2);
		}

		internal unsafe static void Init() {
			var etAddress = Services.SigScanner.ScanText("48 89 5C 24 ?? 57 48 83 EC ?? 48 8B F9 48 8B DA 48 81 C1 ?? ?? ?? ?? E8 ?? ?? ?? ?? 4C 8B 87 ?? ?? ?? ??");
			UpdateEorzeaTimeHook = Hook<UpdateEorzeaTimeDelegate>.FromAddress(etAddress, UpdateEorzeaTime);
			UpdateEorzeaTimeHook.Enable();

			var twAddress = Services.SigScanner.ScanText("48 89 5C 24 ?? 55 56 57 48 83 EC ?? 48 8B F9 48 8D 0D ?? ?? ?? ??");
			UpdateTerritoryWeatherHook = Hook<UpdateTerritoryWeatherDelegate>.FromAddress(twAddress, UpdateTerritoryWeather);
			UpdateTerritoryWeatherHook.Enable();

			IntPtr rawWeather = Services.SigScanner.GetStaticAddressFromSig("4C 8B 05 ?? ?? ?? ?? 41 8B 80 ?? ?? ?? ?? C1 E8 02");
			WeatherSystem = *(WeatherSystem**) rawWeather;

			EventManager.OnGPoseChange += OnGPoseChange;
		}

		internal static void OnGPoseChange(ActorGposeState _state) {
			if (_state == ActorGposeState.OFF) {
				TimeUpdateDisabled = false;
				WeatherUpdateDisabled = false;
			}
		}

		internal static void Dispose() {
			EventManager.OnGPoseChange -= OnGPoseChange;

			UpdateEorzeaTimeHook.Disable();
			UpdateEorzeaTimeHook.Dispose();

			UpdateTerritoryWeatherHook.Disable();
			UpdateTerritoryWeatherHook.Dispose();
		}
	}
}