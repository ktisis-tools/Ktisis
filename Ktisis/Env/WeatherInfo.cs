using Dalamud.Interface.Textures;
using Dalamud.Utility;

using Lumina.Excel.Sheets;

namespace Ktisis.Env {
	public class WeatherInfo {
		public readonly string Name;
		public readonly uint RowId;
		public readonly ISharedImmediateTexture? Icon;

		public WeatherInfo(string name) {
			this.Name = name;
		}
		
		public WeatherInfo(Weather weather, ISharedImmediateTexture? icon) {
			var name = weather.Name.ExtractText();
			if (name.IsNullOrEmpty())
				name = $"Weather #{weather.RowId}";
			
			this.Name = name;
			this.RowId = weather.RowId;
			this.Icon = icon;
		}

		public static WeatherInfo Default => new("None");
	}
}
