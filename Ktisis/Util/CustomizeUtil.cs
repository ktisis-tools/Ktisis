using System;
using System.Collections;
using System.Collections.Generic;

using Dalamud.Game.ClientState.Objects.Enums;

using ImGuiScene;

using Ktisis.GameData;
using Ktisis.GameData.Excel;
using Ktisis.Structs.Actor;

namespace Ktisis.Util {
	internal class CustomizeUtil {
		// Calculate row index

		public static uint GetMakeIndex(Customize custom) {
			var r = (uint)custom.Race;
			var t = (uint)custom.Tribe;
			var g = (uint)custom.Gender;
			var i = Customize.GetRaceTribeIndex(custom.Race);
			return ((r - 1) * 4) + ((t - i) * 2) + g; // Thanks cait
		}

		// Build char creator options

		public static Dictionary<MenuType, List<MenuOption>> GetMenuOptions(uint index) {
			var options = new Dictionary<MenuType, List<MenuOption>>();

			var data = Sheets.GetSheet<CharaMakeType>().GetRow(index);

			if (data != null) {
				for (int i = 0; i < CharaMakeType.MenuCt; i++) {
					var val = data.Menus[i];

					if (val.Index == 0)
						break;

					if (val.Index == CustomizeIndex.EyeColor2)
						continue; // TODO: Heterochromia

					var type = val.Type;
					if (type == MenuType.Unknown1)
						type = MenuType.Color;
					if (type == MenuType.Color)
						continue;

					if (!options.ContainsKey(type))
						options[type] = new();

					var opt = new MenuOption(val);

					var next = data.Menus[i + 1];
					if (next.Type == MenuType.Color)
						opt.Color = next;

					if (val.HasIcon) {
						var icons = new Dictionary<uint, TextureWrap>();
						if (val.IsFeature) {
							foreach (var row in val.Features) {
								var feat = row.Value!;
								var icon = Dalamud.DataManager.GetImGuiTextureHqIcon(feat.Icon);
								if (feat.FeatureId == 0)
									continue;
								icons.Add(feat.FeatureId, icon!);
							}
						} else {
							for (var x = 0; x < val.Count; x++) {
								var icon = Dalamud.DataManager.GetImGuiTextureHqIcon(val.Params[x]);
								icons.Add(val.Graphics[x], icon!);
							}
						}
						opt.Select = icons;
					}

					options[type].Add(opt);
				}
			}

			return options;
		}
	}

	public struct MenuOption {
		public Menu Option;
		public Menu? Color = null;

		public Dictionary<uint, TextureWrap>? Select = null;

		public MenuOption(Menu option) {
			Option = option;
		}
	}
}