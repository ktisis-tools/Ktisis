using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Dalamud.Plugin.Services;
using Dalamud.Interface.Internal;

using Ktisis.Core;

using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;

namespace Ktisis.Data; 

[DIService]
public class PluginData : SchemaReader {
	// Constructor

	private readonly IDataManager _data;
	private readonly ITextureProvider _tex;
	private readonly IClientState _state;

	public PluginData(IDataManager _data, ITextureProvider _tex, IClientState _state) {
		this._data = _data;
		this._tex = _tex;
		this._state = _state;
	}
	
	// Data access

	public async Task<ExcelSheet<T>?> GetSheetAsync<T>() where T : ExcelRow {
		await Task.Yield();
		return this._data.GetExcelSheet<T>();
	}

	public async Task<IDalamudTextureWrap?> GetIconAsync(uint iconId) {
		await Task.Yield();
		return this._tex.GetIcon(iconId);
	}

	public async Task<IDalamudTextureWrap?> GetSkyboxTex(uint skyId) {
		await Task.Yield();
		return this._tex.GetTextureFromGame($"bgcommon/nature/sky/texture/sky_{skyId:000}.tex");
	}
	
	// Zone weather + icons

	public async Task<Dictionary<Weather, IDalamudTextureWrap?>> GetZoneWeatherAndIcons(CancellationToken token) {
		await Task.Yield();
		
		var result = new Dictionary<Weather, IDalamudTextureWrap?>();
		
		var id = this._state.TerritoryType;
		
		var territory = this._data.GetExcelSheet<TerritoryType>()?.GetRow(id);
		if (territory == null || token.IsCancellationRequested) return result;

		var weatherRate = this._data.GetExcelSheet<WeatherRate>()?.GetRow(territory.WeatherRate);
		if (token.IsCancellationRequested) return result;
		var weatherSheet = this._data.GetExcelSheet<Weather>();
		if (weatherRate == null || weatherSheet == null || token.IsCancellationRequested) return result;

		var data = weatherRate.UnkData0.ToList();
		data.Sort((a, b) => a.Weather - b.Weather);
		
		foreach (var rate in data) {
			if (token.IsCancellationRequested) break;
			if (rate.Weather <= 0 || rate.Rate == 0) continue;
			
			var weather = weatherSheet.GetRow((uint)rate.Weather);
			if (weather == null) continue;
			
			var icon = this._tex.GetIcon((uint)weather.Icon);
			result.TryAdd(weather, icon);
		}
		
		return result;
	}
}
