using Dalamud.Interface.Internal;
using Dalamud.Utility;

using Lumina.Excel.GeneratedSheets;

namespace Ktisis.Services.Environment;

public class WeatherInfo {
	public readonly string Name;
	public readonly uint RowId;
	public readonly IDalamudTextureWrap? Icon;

	public WeatherInfo(Weather row, IDalamudTextureWrap? icon) {
		var name = row.Name?.RawString;
		if (name.IsNullOrEmpty())
			name = $"Weather #{row.RowId}";
		this.Name = name;
		this.RowId = row.RowId;
		this.Icon = icon;
	}
	
	private class EmptyTexture : IDalamudTextureWrap {
		public nint ImGuiHandle { get; } = nint.Zero;
		public int Width { get; } = 0;
		public int Height { get; } = 0;
		public void Dispose() { }
	}
}
