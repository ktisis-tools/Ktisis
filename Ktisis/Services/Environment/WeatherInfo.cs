using Dalamud.Interface.Textures;
using Dalamud.Utility;

using Lumina.Excel.GeneratedSheets;

namespace Ktisis.Services.Environment;

public class WeatherInfo {
	public readonly string Name;
	public readonly uint RowId;
	public readonly ISharedImmediateTexture? Icon;

	public WeatherInfo(Weather row, ISharedImmediateTexture? icon) {
		var name = row.Name?.RawString;
		if (name.IsNullOrEmpty())
			name = $"Weather #{row.RowId}";
		this.Name = name;
		this.RowId = row.RowId;
		this.Icon = icon;
	}
}
