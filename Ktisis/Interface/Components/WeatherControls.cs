using System;
using System.Collections.Generic;
using System.Linq;

using Dalamud.Interface;

using ImGuiNET;

using Lumina.Excel.GeneratedSheets;

using Ktisis.Interop.Hooks;
using Ktisis.Util;

namespace Ktisis.Interface.Components {
	public static class WeatherControls {
		private static uint CachedTerritory = 0xFFFFFFFF;
		private static readonly List<(byte Id, string Name)> ZoneValidWeatherList = new();
		private static readonly Lazy<List<Weather>> WeatherSheet = new(() => Services.DataManager.GameData.GetExcelSheet<Weather>()!.Where(i => !string.IsNullOrEmpty(i.Name)).ToList());
		private static bool SearchOpen = false;
		private static string SearchTerm = string.Empty;

		public unsafe static void Draw() {
			if (ImGui.CollapsingHeader("Weather Control")) {
				int weatherId = WorldHooks.WeatherSystem->CurrentWeather;
				int originalWeatherId = weatherId;

				UpdateCache();

				ImGui.Checkbox("Lock Weather", ref WorldHooks.WeatherUpdateDisabled);

				bool isLocked = WorldHooks.WeatherUpdateDisabled;

				if (!isLocked) ImGui.BeginDisabled();

				ImGui.SetNextItemWidth(130f);
				ImGui.InputInt("Weather ID", ref weatherId, 0, 0);
				ImGui.SameLine();
				if (GuiHelpers.IconButtonTooltip(FontAwesomeIcon.Search, "Search"))
					SearchOpen = true;

				ImGui.PushItemWidth(ImGui.GetContentRegionAvail().X - GuiHelpers.WidthMargin());
				if (ImGui.BeginListBox("###weather_list")) {

					foreach (var weather in ZoneValidWeatherList) {
						bool isSelected = weather.Id == weatherId;
						if (ImGui.Selectable($"{weather.Name} ({weather.Id})###weather_{weather.Id}", isSelected)) {
							weatherId = weather.Id;
						}
					}

					ImGui.EndListBox();
				}
				ImGui.PopItemWidth();

				if (isLocked && weatherId != originalWeatherId)
					SetWeather((ushort)weatherId);

				if (!isLocked) ImGui.EndDisabled();

				if (SearchOpen)
					DrawWeatherSearchPopup();
			}
		}

		private static void UpdateCache() {

			ushort territoryId = Services.ClientState.TerritoryType;

			if (CachedTerritory == territoryId)
				return;

			ZoneValidWeatherList.Clear();
			CachedTerritory = territoryId; // We set this here so if there is a failure we don't try again until a rezone

			var territory = Services.DataManager.GameData.GetExcelSheet<TerritoryType>()!.GetRow(territoryId);

			if (territory == null)
				return;

			var rate = Services.DataManager.GameData.GetExcelSheet<WeatherRate>()!.GetRow(territory.WeatherRate);

			if (rate == null)
				return;

			foreach (var wr in rate!.UnkData0) {
				if (wr.Weather == 0)
					continue;

				var weather = WeatherSheet.Value.SingleOrDefault(i => i.RowId == wr.Weather);

				if (weather == null)
					continue;

				if (ZoneValidWeatherList.Count(x => x.Id == (byte)weather.RowId) == 0)
					ZoneValidWeatherList.Add(((byte)weather.RowId, weather.Name));

			}

			ZoneValidWeatherList.Sort((x, y) => x.Id.CompareTo(y.Id));
		}

		private unsafe static void DrawWeatherSearchPopup() {
			PopupSelect.HoverPopupWindow(
				PopupSelect.HoverPopupWindowFlags.SelectorList | PopupSelect.HoverPopupWindowFlags.SearchBar,
				WeatherSheet.Value!,
				(e, input) => e.Where(t => $"{t.Name} ({t.RowId})".Contains(input, StringComparison.OrdinalIgnoreCase)),
				(t, a) => {
					// draw Line
					bool selected = ImGui.Selectable($"{t.Name} ({t.RowId})###weather_item_{t.RowId}", a);
					bool focus = ImGui.IsItemFocused();
					return (selected, focus);
				},
				(t) => SetWeather((ushort)t.RowId),
				() => SearchOpen = false,
				ref SearchTerm,
				"Weather Select",
				"##weather_select",
				"##weather_search");
			;
		}

		private unsafe static void SetWeather(ushort weatherId) => WorldHooks.WeatherSystem->CurrentWeather = weatherId;
	}
}