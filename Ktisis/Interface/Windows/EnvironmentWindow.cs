using System;
using System.Numerics;
using System.Threading;
using System.Collections.Generic;

using Dalamud.Interface.Internal;
using Dalamud.Interface.Windowing;

using FFXIVClientStructs.FFXIV.Client.Graphics.Environment;

using ImGuiNET;

using Ktisis.Data;
using Ktisis.Environment;
using Ktisis.Localization;
using Ktisis.Core.Services;
using Ktisis.Common.Extensions;
using Ktisis.Interop.Structs.Scene;

using Lumina.Excel.GeneratedSheets;

namespace Ktisis.Interface.Windows; 

public class EnvironmentWindow : Window {
	private readonly DataService _data;
	private readonly LocaleService _locale;
	private readonly GPoseService _gpose;
	private readonly EnvService _env;
	
	public EnvironmentWindow(DataService _data, LocaleService _locale, GPoseService _gpose, EnvService _env) : base(
		"##__EnvEditor__"
	) {
		this._data = _data;
		this._locale = _locale;
		this._gpose = _gpose;
		this._env = _env;

		SizeConstraints = new WindowSizeConstraints {
			MinimumSize = new Vector2(250, 150),
			MaximumSize = ImGui.GetIO().DisplaySize * 0.9f
		};
	}
	
	// Data

	private CancellationTokenSource? TokenSource;

	private Dictionary<Weather, IDalamudTextureWrap?> Weather = new();
	
	// Window open handler - fetch async data here

	public override void OnOpen() {
		this.WindowName = this._locale.Translate("env_edit.title");

		var source = new CancellationTokenSource();
		var token = source.Token;
		this.TokenSource = source;
		this._data.GetZoneWeatherAndIcons(token).ContinueWith(result => {
			if (result.Exception != null) {
				Ktisis.Log.Error($"Failed to load weather data:\n{result.Exception}");
				return;
			}

			lock (this) {
				this.Weather = result.Result;
				this.TokenSource = null;
			}
		}, token);
	}

	public override void PreOpenCheck() {
		if (!this._gpose.IsInGPose)
			this.Close();
	}
	
	// UI Draw

	private readonly static Vector2 WeatherIconSize = new(28, 28);

	public unsafe override void Draw() {
		var env = EnvManager.Instance();
		var val = this._env.GetOverride();
		if (env == null || val == null) return;

		/*ImGui.Checkbox("Enable advanced editing", ref val.Advanced);
		ImGui.Spacing();
		ImGui.Separator();
		ImGui.Spacing();*/

		var time = new DateTime().AddSeconds(val.Props.Time);
		ImGui.SliderFloat("Time", ref val.Props.Time, 0, 86400, time.ToShortTimeString(), ImGuiSliderFlags.NoInput);
		
		ImGui.Spacing();
		
		lock (this) {
			var disable = this.Weather.Count <= 1;
			ImGui.BeginDisabled(disable);
			if (DrawWeatherSelect(env->ActiveWeather, out var clickedId))
				env->ActiveWeather = (byte)clickedId;
			if (disable)
				ImGui.Text("Weather unavailable in this area.");
			ImGui.EndDisabled();
		}
		
		DrawAdvanced(ref val.Props);
	}
	
	// Weather select

	private bool DrawWeatherSelect(int current, out int clickedId) {
		var click = false;
		clickedId = 0;
		
		var style = ImGui.GetStyle();
		var padding = style.FramePadding.Y + WeatherIconSize.Y - ImGui.GetFrameHeight();
		
		ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, style.FramePadding with { Y = padding });
		if (ImGui.BeginCombo("Weather", current != 0 ? "##" : "No Weather")) {
			foreach (var (weatherInfo, icon) in this.Weather) {
				if (ImGui.Selectable($"##EnvWeather{weatherInfo.RowId}", weatherInfo.RowId == current)) {
					click = true;
					clickedId = (int)weatherInfo.RowId;
				}
				DrawWeatherLabel(weatherInfo, icon, true);
			}
			
			ImGui.EndCombo();
		}
		
		ImGui.SameLine();

		foreach (var (weather, icon) in this.Weather) {
			if (weather.RowId != (uint)current) continue;
			DrawWeatherLabel(weather, icon);
			break;
		}
		
		ImGui.PopStyleVar();

		return click;
	}

	private void DrawWeatherLabel(Weather weather, IDalamudTextureWrap? icon, bool adjustPad = false) {
		var style = ImGui.GetStyle();
		var height = ImGui.GetFrameHeight();
		
		if (icon != null) {
			ImGui.SameLine();
			ImGui.SetCursorPosX(ImGui.GetCursorStartPos().X + style.ItemInnerSpacing.X);

			var posY = ImGui.GetCursorPos().Y + height / 2 - WeatherIconSize.Y / 2;
			if (adjustPad) posY -= style.FramePadding.Y;
			ImGui.SetCursorPosY(posY);
			
			ImGui.Image(icon.ImGuiHandle, WeatherIconSize);
			ImGui.SameLine();
		}
		ImGui.Text(weather.Name);
	}
	
	// Advanced properties

	private void DrawAdvanced(ref EnvProps val) {
		/*ImGui.Spacing();
		ImGui.Text("Advanced");
		ImGui.Separator();*/
		ImGui.Spacing();
		
		DrawSkySelect(ref val);
		
		ImGui.Spacing();

		return; // TODO

		ImGui.ColorEdit3("Fog Color", ref val.FogColor);
		ImGui.SliderFloat("Fog Distance", ref val.Fog1, 0, 100);
		ImGui.SliderFloat("Fog Opacity", ref val.Fog2, 0, 0.25f);
		ImGui.SliderFloat("Fog 3", ref val.Fog3, 0, 1);
	}
	
	// Sky select

	private readonly object SkyLock = new();
	
	private uint CurSky = 1;
	private IDalamudTextureWrap? SkyTex;

	private void GetSkyImage(EnvProps val) {
		var sky = val.SkyId;
		if (sky == this.CurSky) return;
		
		this.CurSky = sky;
		this._data.GetSkyboxTex(this.CurSky).ContinueWith(result => {
			if (result.Exception != null) {
				Ktisis.Log.Error(result.Exception.ToString());
				return;
			}

			lock (this.SkyLock) {
				this.SkyTex?.Dispose();
				this.SkyTex = result.Result;
			}
		});
	}
	
	private void DrawSkySelect(ref EnvProps val) {
		GetSkyImage(val);

		var innerSpace = ImGui.GetStyle().ItemInnerSpacing.Y;
		
		var height = ImGui.GetFrameHeight() * 2 + innerSpace;
		var buttonSize = new Vector2(height, height);

		//var button = false;
		lock (this.SkyLock) {
			if (this.SkyTex != null)
				ImGui.Image(this.SkyTex.ImGuiHandle, buttonSize);
		}

		ImGui.SameLine();

		ImGui.SetCursorPosY(ImGui.GetCursorPosY() + innerSpace);
		
		ImGui.BeginGroup();
		ImGui.Text("Sky Texture");
		var sky = (int)val.SkyId;
		ImGui.SetNextItemWidth(ImGui.CalcItemWidth() - (ImGui.GetCursorPosX() - ImGui.GetCursorStartPos().X));
		if (ImGui.InputInt("##SkyId", ref sky))
			val.SkyId = (uint)sky;
		ImGui.EndGroup();
	}
	
	// Window close handler - dispose here

	public override void OnClose() {
		lock (this) {
			this.TokenSource?.Cancel();
		}
		
		foreach (var icon in this.Weather.Values)
			icon?.Dispose();
		this.Weather.Clear();
	}
}
