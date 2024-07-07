using System;
using System.Numerics;
using System.Threading;
using System.Collections.Generic;
using System.Linq;

using FFXIVClientStructs.FFXIV.Client.Graphics.Environment;

using ImGuiNET;

using Ktisis.Env;
using Ktisis.Structs.Env;

namespace Ktisis.Interface.Windows.Workspace.Tabs {
	public static class WorldTab {
		// Data

		private static object AsyncLock = new();
		
		private static CancellationTokenSource? TokenSource;

		private static ushort TerritoryType = ushort.MaxValue;
		private static List<WeatherInfo> Weather = new();
		
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

			var weather = EnvService.GetEnvWeatherIds();
			EnvService.GetWeatherIcons(weather, token).ContinueWith(result => {
				if (result.Exception != null) {
					Ktisis.Log.Error($"Failed to load weather data:\n{result.Exception}");
					return;
				} else if (result.IsCanceled) return;

				lock (AsyncLock) {
					Weather = result.Result.Prepend(WeatherInfo.Default).ToList();
					TokenSource = null;
				}
			}, token);
			
			Logger.Information($"Weathers: {string.Join(", ", weather)}");
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
			
			ImGui.Spacing();

			var waterFrozen = EnvService.FreezeWater ?? false;
			if (ImGui.Checkbox("Freeze Water", ref waterFrozen)) {
				EnvService.FreezeWater = waterFrozen;
			}
		}
		
		// Weather
		
		private static readonly Vector2 WeatherIconSize = new(28, 28);
		
		private static bool DrawWeatherSelect(int current, out int clickedId) {
			var currentInfo = Weather.Find(w => w.RowId == current);
			
			var click = false;
			clickedId = 0;
            
			var style = ImGui.GetStyle();
			var padding = style.FramePadding.Y + WeatherIconSize.Y - ImGui.GetFrameHeight();

			ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - style.ItemInnerSpacing.X - LabelMargin);
			ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, style.FramePadding with { Y = padding });
			if (ImGui.BeginCombo(WeatherLabel, currentInfo != null ? "##" : "Unknown")) {
				foreach (var weatherInfo in Weather) {
					if (ImGui.Selectable($"##EnvWeather{weatherInfo.RowId}", weatherInfo.RowId == current, ImGuiSelectableFlags.DontClosePopups)) {
						click = true;
						clickedId = (int)weatherInfo.RowId;
					}
					DrawWeatherLabel(weatherInfo, true);
				}
            
				ImGui.EndCombo();
			}
            
			if (currentInfo != null)
				DrawWeatherLabel(currentInfo);
		
			ImGui.PopStyleVar();

			return click;
		}

		private static void DrawWeatherLabel(WeatherInfo weather, bool adjustPad = false) {
			var style = ImGui.GetStyle();
			var height = ImGui.GetFrameHeight();
			
			ImGui.SameLine(0, 0);
			ImGui.SetCursorPosX(ImGui.GetCursorStartPos().X + style.ItemInnerSpacing.X);

			var posY = ImGui.GetCursorPos().Y + height / 2 - WeatherIconSize.Y / 2;
			if (adjustPad) posY -= style.FramePadding.Y;
			ImGui.SetCursorPosY(posY);
			
			ImGui.Image(weather.Icon?.GetWrapOrEmpty().ImGuiHandle ?? 0, WeatherIconSize);
			ImGui.SameLine();
            
			ImGui.Text(weather.Name);
		}
		
		// Sky
	
		private static bool DrawSkySelect(ref uint skyId) {
			EnvService.GetSkyImage(skyId);

			var innerSpace = ImGui.GetStyle().ItemInnerSpacing.Y;
		
			var height = ImGui.GetFrameHeight() * 2 + innerSpace;
			var buttonSize = new Vector2(height, height);
            
			lock (EnvService.SkyLock) {
				if (EnvService.SkyTex != null)
					ImGui.Image(EnvService.SkyTex.GetWrapOrEmpty().ImGuiHandle, buttonSize);
			}

			ImGui.SameLine();

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
	}
}
