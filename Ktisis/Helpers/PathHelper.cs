using System;
using System.Collections.Generic;

using Lumina.Excel.GeneratedSheets;


namespace Ktisis.Helpers
{
	public static class PathHelper {
		
		internal static readonly Dictionary<string, Func<string>> Replacers = new() {
			{"%Date%", () => DateTime.Now.ToString("yyyy-MM-dd")},
			{"%Year%", () => DateTime.Now.ToString("yyyy")},
			{"%Month%", () => DateTime.Now.ToString("MM")},
			{"%Day%", () => DateTime.Now.ToString("dd")},
			{"%Time%", () => DateTime.Now.ToString("hh-mm-ss")},
			{"%PlayerName%", () => {
				if (Ktisis.Configuration.DisplayCharName)
					return Services.ClientState.LocalPlayer?.Name.ToString() ?? "Unknown";

				return "Player";
			}},
			{"%CurrentWorld%", () => Services.ClientState.LocalPlayer?.CurrentWorld.GameData?.Name.ToString() ?? "Unknown"},
			{"%HomeWorld%", () => Services.ClientState.LocalPlayer?.HomeWorld.GameData?.Name.ToString() ?? "Unknown" },
			{"%Zone%", () => Services.DataManager.GetExcelSheet<TerritoryType>()?.GetRow(Services.ClientState.TerritoryType)?.PlaceName.Value?.Name.ToString() ?? "Unknown"},
		};
		
		internal static string Replace(string path) {
			if (string.IsNullOrEmpty(path)) return path;
			if (!path.Contains('%')) return path;
			
			foreach ((var key, var value) in Replacers) {
				path = path.Replace(key, value());
			}

			return path;
		}
	}
}
