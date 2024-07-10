using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Dalamud.Plugin.Services;

using Ktisis.Core.Attributes;
using Ktisis.Structs.Env;

using Lumina.Excel.GeneratedSheets;

namespace Ktisis.Services.Environment;

[Singleton]
public class WeatherService {
	private readonly IDataManager _data;
	private readonly IFramework _framework;
	private readonly ITextureProvider _texture;
	
	public WeatherService(
		IDataManager data,
		IFramework framework,
		ITextureProvider texture
	) {
		this._data = data;
		this._framework = framework;
		this._texture = texture;
	}

	public async Task<IEnumerable<WeatherInfo>> GetWeatherTypes(CancellationToken token) {
		await Task.Yield();

		var results = new List<WeatherInfo>();

		var weathers = this._framework.RunOnFrameworkThread(this.GetEnvWeatherIds);

		var weatherSheet = this._data.GetExcelSheet<Weather>();
		if (weatherSheet == null) return results;
		
		foreach (var id in await weathers) {
			if (token.IsCancellationRequested) break;

			var weather = weatherSheet.GetRow(id);
			if (weather == null) continue;

			var icon = this._texture.GetFromGameIcon((uint)weather.Icon);
			var info = new WeatherInfo(weather, icon);
			results.Add(info);
		}
		
		token.ThrowIfCancellationRequested();
		
		return results;
	}
	
	public unsafe byte[] GetEnvWeatherIds() {
		var env = EnvManagerEx.Instance();
		var scene = env != null ? env->_base.EnvScene : null;
		if (scene == null) return Array.Empty<byte>();
		return scene->WeatherIds
			.TrimEnd((byte)0)
			.ToArray();
	}
}
