using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Utility;

using Lumina.Excel.GeneratedSheets;

namespace Ktisis.Env {
	public class WeatherInfo {
		public readonly string Name;
		public readonly uint RowId;
		public readonly IDalamudTextureWrap? Icon;

		public WeatherInfo(string name) {
			this.Name = name;
			this.Icon = new NullTexture();
		}
		
		public WeatherInfo(Weather weather, IDalamudTextureWrap? icon) {
			var name = weather.Name?.RawString;
			if (name.IsNullOrEmpty())
				name = $"Weather #{weather.RowId}";
			
			this.Name = name;
			this.RowId = weather.RowId;
			this.Icon = icon;
		}

		public static WeatherInfo Default => new("None");

		private class NullTexture : IDalamudTextureWrap {
			public nint ImGuiHandle { get; } = nint.Zero;
			public int Width { get; } = 0;
			public int Height { get; } = 0;
			
			public void Dispose() { }
		}
	}
}
