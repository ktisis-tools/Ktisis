using System.Collections.Generic;
using System.Numerics;
using System.Threading;

using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin.Services;

using ImGuiNET;

using Ktisis.Core.Attributes;
using Ktisis.Services.Environment;
using Ktisis.Structs.Env;

namespace Ktisis.Interface.Components.Environment;

[Transient]
public class WeatherSelect {
	private readonly static Vector2 WeatherIconSize = new(28, 28);
    
	private readonly IClientState _clientState;
	private readonly WeatherService _weather;

	private readonly WeatherResource _resource;
    
	public WeatherSelect(
		IClientState clientState,
		WeatherService weather
	) {
		this._clientState = clientState;
		this._weather = weather;
		this._resource = new WeatherResource(weather);
	}
	
	public unsafe bool Draw(EnvManagerEx* env, out WeatherInfo? selected) {
		selected = null;
		if (env == null) return false;
		
		var weathers = this._resource.Get(this._clientState.TerritoryType);

		var currentId = env->_base.ActiveWeather;
		var current = this._resource.Find(currentId);

		var style = ImGui.GetStyle();
		var padding = style.FramePadding.Y + WeatherIconSize.Y - ImGui.GetFrameHeight();
		using var _style = ImRaii.PushStyle(ImGuiStyleVar.FramePadding, style.FramePadding with { Y = padding });
		
		var result = this.DrawWeatherCombo(currentId, current, weathers, out selected);
		if (current != null)
			this.DrawWeatherLabel(current, false);
		return result;
	}

	private bool DrawWeatherCombo(byte id, WeatherInfo? current, IEnumerable<WeatherInfo> weathers, out WeatherInfo? selected) {
		selected = null;
		
		ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
		using var _combo = ImRaii.Combo("##WeatherCombo", current != null ? "##" : "Unknown");
		if (!_combo.Success) return false;

		var clicked = false;
		foreach (var weather in weathers) {
			var activate = ImGui.Selectable($"##EnvWeather_{weather.RowId}", weather.RowId == id);
			this.DrawWeatherLabel(weather, true);
			if (activate) selected = weather;
			clicked |= activate;
		}
		return clicked;
	}

	private void DrawWeatherLabel(WeatherInfo weather, bool adjustPad) {
		var style = ImGui.GetStyle();
		var height = ImGui.GetFrameHeight();

		if (weather.Icon != null) {
			ImGui.SameLine(0, 0);
			ImGui.SetCursorPosX(ImGui.GetCursorStartPos().X + style.ItemInnerSpacing.X);

			var posY = ImGui.GetCursorPosY() + height / 2 - WeatherIconSize.Y / 2;
			if (adjustPad) posY -= style.FramePadding.Y;
			ImGui.SetCursorPosY(posY);
			
			ImGui.Image(weather.Icon.GetWrapOrEmpty().ImGuiHandle, WeatherIconSize);
			ImGui.SameLine();
		}

		ImGui.Text(weather.Name);
	}
	
	// Weather Resources

	private class WeatherResource(WeatherService service) {
		private uint TerritoryId;
		private readonly List<WeatherInfo> Cached = new();

		private CancellationTokenSource? TokenSource;
		
		public IEnumerable<WeatherInfo> Get(uint territory) {
			if (territory != this.TerritoryId) {
				this.TerritoryId = territory;
				this.Fetch();
			}

			lock (this.Cached)
				return this.Cached;
		}

		public WeatherInfo? Find(int rowId) {
			lock (this.Cached)
				return this.Cached.Find(row => row.RowId == rowId);
		}

		private void Fetch() {
			this.TokenSource?.Dispose();
			this.TokenSource = new CancellationTokenSource();
			service.GetWeatherTypes(this.TokenSource.Token).ContinueWith(task => {
				if (task.Exception != null) {
					Ktisis.Log.Error($"Failed to fetch weather:\n{task.Exception}");
					return;
				}

				lock (this.Cached) {
					this.Cached.Clear();
					this.Cached.AddRange(task.Result);
				}
			});
		}
	}
}
