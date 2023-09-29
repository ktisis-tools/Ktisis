using System;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Dalamud.Interface.Internal;
using Dalamud.Logging;

using FFXIVClientStructs.FFXIV.Client.Graphics.Environment;

using ImGuiNET;

using Ktisis.Env;
using Ktisis.Structs.Env;

using Lumina.Excel.GeneratedSheets;

namespace Ktisis.Interface.Windows.Workspace.Tabs {
	public static class WorldTab {
		// Data

		private static object AsyncLock = new();
		
		private static CancellationTokenSource? TokenSource;

		private static ushort TerritoryType = ushort.MaxValue;
		private static Dictionary<Weather, IDalamudTextureWrap?> Weather = new();
		
		// UI Draw

		private static float LabelMargin = 0.0f;

		private const string TimeLabel = "Time";
		private const string WeatherLabel = "Weather";

		private static void CheckData() {
			var territory = Services.ClientState.TerritoryType;
			if (territory == TerritoryType) return;
			TerritoryType = territory;
				
			var source = new CancellationTokenSource();
			var token = source.Token;
			TokenSource?.Cancel();
			TokenSource = source;

			GetZoneWeatherAndIcons(territory, token).ContinueWith(result => {
				if (result.Exception != null) {
					PluginLog.Error($"Failed to load weather data:\n{result.Exception}");
					return;
				} else if (result.IsCanceled) return;

				lock (AsyncLock) {
					Weather = result.Result;
					TokenSource = null;
				}
			}, token);
		}
		
		public static void Draw() {
			CheckData();

			LabelMargin = Math.Max(
				ImGui.CalcTextSize(TimeLabel).X,
				ImGui.CalcTextSize(WeatherLabel).X
			);
			
			ImGui.Spacing();
            
			DrawControls();
			
			ImGui.EndTabItem();
		}

		private unsafe static void DrawControls() {
			var env = (EnvManagerEx*)EnvManager.Instance();
			if (env == null) return;
            
			var time = EnvService.TimeOverride ?? env->Time;
			var sky = EnvService.SkyOverride ?? env->SkyId;

			var width = ImGui.GetContentRegionAvail().X - LabelMargin;
			var spacing = ImGui.GetStyle().ItemInnerSpacing.X;

			const float MaxTime = 86400;
			
			var dateTime = new DateTime().AddSeconds(time);
			ImGui.SetNextItemWidth(width * (3f/4f) - spacing * 2);
			if (ImGui.SliderFloat($"##{TimeLabel}_Slider", ref time, 0, 86400, dateTime.ToShortTimeString(), ImGuiSliderFlags.NoInput))
				EnvService.TimeOverride = time % MaxTime;

			var timeMins = time / 60f;
			ImGui.SameLine(0, spacing);
			ImGui.SetNextItemWidth(width * (1f/4f));
			if (ImGui.InputFloat(TimeLabel, ref timeMins, 0, 0, "%.0f"))
				EnvService.TimeOverride = (timeMins * 60f) % MaxTime;
			
			ImGui.Spacing();
			
			lock (AsyncLock) {
				var disable = Weather.Count <= 1;
				ImGui.BeginDisabled(disable);
				if (DrawWeatherSelect(env->ActiveWeather, out var clickedId))
					env->ActiveWeather = (byte)clickedId;
				if (disable)
					ImGui.Text("Weather unavailable in this area.");
				ImGui.EndDisabled();
			}
			
			ImGui.Spacing();

			var skyId = EnvService.SkyOverride ?? env->SkyId;
			if (DrawSkySelect(ref skyId))
				EnvService.SkyOverride = skyId;
		}
		
		// Weather
		
		private static readonly Vector2 WeatherIconSize = new(28, 28);
		
		private static bool DrawWeatherSelect(int current, out int clickedId) {
			var click = false;
			clickedId = 0;
            
			var style = ImGui.GetStyle();
			var padding = style.FramePadding.Y + WeatherIconSize.Y - ImGui.GetFrameHeight();

			ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - style.ItemInnerSpacing.X - LabelMargin);
			ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, style.FramePadding with { Y = padding });
			if (ImGui.BeginCombo(WeatherLabel, current != 0 ? "##" : "No Weather")) {
				foreach (var (weatherInfo, icon) in Weather) {
					if (ImGui.Selectable($"##EnvWeather{weatherInfo.RowId}", weatherInfo.RowId == current)) {
						click = true;
						clickedId = (int)weatherInfo.RowId;
					}
					DrawWeatherLabel(weatherInfo, icon, true);
				}
            
				ImGui.EndCombo();
			}

			foreach (var (weather, icon) in Weather) {
				if (weather.RowId != (uint)current) continue;
				DrawWeatherLabel(weather, icon);
				break;
			}
		
			ImGui.PopStyleVar();

			return click;
		}

		private static void DrawWeatherLabel(Weather weather, IDalamudTextureWrap? icon, bool adjustPad = false) {
			var style = ImGui.GetStyle();
			var height = ImGui.GetFrameHeight();
		
			if (icon != null) {
				ImGui.SameLine(0, 0);
				ImGui.SetCursorPosX(ImGui.GetCursorStartPos().X + style.ItemInnerSpacing.X);

				var posY = ImGui.GetCursorPos().Y + height / 2 - WeatherIconSize.Y / 2;
				if (adjustPad) posY -= style.FramePadding.Y;
				ImGui.SetCursorPosY(posY);
			
				ImGui.Image(icon.ImGuiHandle, WeatherIconSize);
				ImGui.SameLine();
			}
			ImGui.Text(weather.Name);
		}
		
		// Sky

		private static uint CurSky;

		private static readonly object SkyLock = new();
		private static IDalamudTextureWrap? SkyTex;

		private static void GetSkyImage(uint sky) {
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
	
		private static bool DrawSkySelect(ref uint skyId) {
			GetSkyImage(skyId);

			var innerSpace = ImGui.GetStyle().ItemInnerSpacing.Y;
		
			var height = ImGui.GetFrameHeight() * 2 + innerSpace;
			var buttonSize = new Vector2(height, height);

			//var button = false;
			lock (SkyLock) {
				if (SkyTex != null)
					ImGui.Image(SkyTex.ImGuiHandle, buttonSize);
			}

			ImGui.SameLine();

			var avail = ImGui.GetContentRegionAvail().X;

			ImGui.SetCursorPosY(ImGui.GetCursorPosY() + innerSpace);
		
			ImGui.BeginGroup();
			ImGui.Text("Sky Texture");
			var sky = (int)skyId;
			ImGui.SetNextItemWidth(ImGui.CalcItemWidth() - (ImGui.GetCursorPosX() - ImGui.GetCursorStartPos().X) - LabelMargin);
			var changed = ImGui.InputInt("##SkyId", ref sky);
			if (changed) skyId = (uint)sky;
			ImGui.EndGroup();

			return changed;
		}
		
		// Data
		
		private static async Task<IDalamudTextureWrap?> GetSkyboxTex(uint skyId) {
			await Task.Yield();
			PluginLog.Verbose($"Retrieving skybox texture: {skyId:000}");
			return Services.Textures.GetTextureFromGame($"bgcommon/nature/sky/texture/sky_{skyId:000}.tex");
		}
		
		private static async Task<Dictionary<Weather, IDalamudTextureWrap?>> GetZoneWeatherAndIcons(ushort id, CancellationToken token) {
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
