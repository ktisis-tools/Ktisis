using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Dalamud.Interface.Textures;

using FFXIVClientStructs.FFXIV.Client.Graphics.Environment;

using Ktisis.Events;
using Ktisis.Interop.Hooks;
using Ktisis.Structs.Env;

using Lumina.Excel.Sheets;

namespace Ktisis.Env {
	public static class EnvService {
		public static float? TimeOverride;
		public static uint? SkyOverride;
		public static bool? FreezeWater;
		
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
			Ktisis.Log.Info($"Setting env hooks: {state}");
			EnvHooks.SetEnabled(state);
			if (!state) {
				TimeOverride = null;
				SkyOverride = null;
				FreezeWater = null;
			}
		}
		
		// Data
		
		private static uint CurSky = uint.MaxValue;

		public static readonly object SkyLock = new();
		public static ISharedImmediateTexture? SkyTex;
		
		public static void GetSkyImage(uint sky) {
			if (sky == CurSky) return;
		
			CurSky = sky;
			GetSkyboxTex(CurSky).ContinueWith(result => {
				if (result.Exception != null) {
					Ktisis.Log.Error(result.Exception.ToString());
					return;
				}

				lock (SkyLock) {
					SkyTex = result.Result;
				}
			});
		}
		
		private static async Task<ISharedImmediateTexture> GetSkyboxTex(uint skyId) {
			await Task.Yield();
			Ktisis.Log.Verbose($"Retrieving skybox texture: {skyId:000}");
			return Services.Textures.GetFromGame($"bgcommon/nature/sky/texture/sky_{skyId:000}.tex");
		}
		
		public unsafe static byte[] GetEnvWeatherIds() {
			var env = (EnvManagerEx*)EnvManager.Instance();
			var scene = env != null ? env->EnvScene : null;
			if (scene == null) return Array.Empty<byte>();
			return scene->GetWeatherSpan()
				.TrimEnd((byte)0)
				.ToArray();
		}

		public static async Task<IEnumerable<WeatherInfo>> GetWeatherIcons(IEnumerable<byte> weathers, CancellationToken token) {
			await Task.Yield();
			
			var result = new List<WeatherInfo>();

			var weatherSheet = Services.DataManager.GetExcelSheet<Weather>();
			if (weatherSheet == null) return result;

			foreach (var id in weathers) {
				if (token.IsCancellationRequested) break;

				if (id == 0) continue;
				
				var weather = weatherSheet.GetRow(id);

				var icon = Services.Textures.GetFromGameIcon((uint)weather.Icon);
				var info = new WeatherInfo(weather, icon);
				result.Add(info);
			}
			
			token.ThrowIfCancellationRequested();
			
			return result;
		}
	}
}
