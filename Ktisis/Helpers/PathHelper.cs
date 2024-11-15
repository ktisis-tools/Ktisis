using System;
using System.Collections.Generic;
using System.Text;

using Lumina.Excel.Sheets;

namespace Ktisis.Helpers {
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
			{"%CurrentWorld%", () => Services.ClientState.LocalPlayer?.CurrentWorld.Value.Name.ToString() ?? "Unknown"},
			{"%HomeWorld%", () => Services.ClientState.LocalPlayer?.HomeWorld.Value.Name.ToString() ?? "Unknown" },
			{"%Zone%", () => Services.DataManager.GetExcelSheet<TerritoryType>()?.GetRow(Services.ClientState.TerritoryType).PlaceName.Value.Name.ToString() ?? "Unknown"},
		};
		
		internal static string Replace(ReadOnlySpan<char> path) {
			StringBuilder output = new StringBuilder(path.Length);

			for (var i = 0; i < path.Length; i++) {
				if (path[i] == '%') {
					for (var j = i + 1; j < path.Length; j++) {
						if (path[j] != '%')
							continue;
						
						var key = path[i..(j + 1)].ToString();

						if (Replacers.TryGetValue(key, out var replacer)) {
							output.Append(replacer());
							i = j;
						} else if (key == "%%") {
							output.Append('%');
							i = j;
						} else {
							output.Append(key);
							i = j - 1; // -1 so that if there is an invalid replacement key, we might be able to show the rest without any issues.
						}

						break;
					}
				} else {
					
					output.Append(path[i]);
				}
			}

			return output.ToString();
		}
	}
}
