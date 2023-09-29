using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Dalamud.Interface.Internal;
using Dalamud.Logging;

using Ktisis.Events;
using Ktisis.Interop.Hooks;

using Lumina.Excel.GeneratedSheets;

namespace Ktisis.Env {
	public static class EnvService {
		public static float? TimeOverride;
		public static uint? SkyOverride;
		
		// Init & Dispose
		
		public static void Init() {
			EventManager.OnGPoseChange += OnGPoseChange;
			EnvHooks.Init();
		}

		public static void Dispose() {
			EventManager.OnGPoseChange -= OnGPoseChange;
			EnvHooks.Dispose();
		}
		
		// Events
		
		private static void OnGPoseChange(bool state) {
			EnvHooks.SetEnabled(state);
			if (!state) {
				TimeOverride = null;
				SkyOverride = null;
			}
		}
		
		// Data
		
		private static uint CurSky = uint.MaxValue;

		public static readonly object SkyLock = new();
		public static IDalamudTextureWrap? SkyTex;
		
		public static void GetSkyImage(uint sky) {
			if (sky == CurSky) return;
		
			CurSky = sky;
			GetSkyboxTex(CurSky).ContinueWith(result => {
				if (result.Exception != null) {
					PluginLog.Error(result.Exception.ToString());
					return;
				}

				lock (SkyLock) {
					SkyTex?.Dispose();
					SkyTex = result.Result;
				}
			});
		}
		
		private static async Task<IDalamudTextureWrap?> GetSkyboxTex(uint skyId) {
			await Task.Yield();
			PluginLog.Verbose($"Retrieving skybox texture: {skyId:000}");
			return Services.Textures.GetTextureFromGame($"bgcommon/nature/sky/texture/sky_{skyId:000}.tex");
		}
		
		public static async Task<Dictionary<Weather, IDalamudTextureWrap?>> GetZoneWeatherAndIcons(ushort id, CancellationToken token) {
			await Task.Yield();
			
			PluginLog.Verbose($"Retrieving weather data for territory: {id}");
		
			var result = new Dictionary<Weather, IDalamudTextureWrap?>();
		
			var territory = Services.DataManager.GetExcelSheet<TerritoryType>()?.GetRow(id);
			if (territory == null || token.IsCancellationRequested) return result;

			var weatherRate = Services.DataManager.GetExcelSheet<WeatherRate>()?.GetRow(territory.WeatherRate);
			if (token.IsCancellationRequested) return result;
			var weatherSheet = Services.DataManager.GetExcelSheet<Weather>();
			if (weatherRate == null || weatherSheet == null || token.IsCancellationRequested) return result;

			var data = weatherRate.UnkData0.ToList();
			data.Sort((a, b) => a.Weather - b.Weather);
		
			foreach (var rate in data) {
				if (token.IsCancellationRequested) break;
				if (rate.Weather <= 0 || rate.Rate == 0) continue;
			
				var weather = weatherSheet.GetRow((uint)rate.Weather);
				if (weather == null) continue;
			
				var icon = Services.Textures.GetIcon((uint)weather.Icon);
				result.TryAdd(weather, icon);
			}
		
			return result;
		}
	}
}
