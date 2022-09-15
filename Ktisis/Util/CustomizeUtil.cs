using System;
using System.Collections;
using System.Collections.Generic;

using Dalamud.Data;
using Dalamud.Game.ClientState.Objects.Enums;

using ImGuiScene;

using Ktisis.Data;
using Ktisis.Structs.Actor;

namespace Ktisis.Util {
	internal class CustomizeUtil {
		public Ktisis Plugin;
		public DataManager Data;

		public CharaMakeType? Cached;
		public Dictionary<MenuType, List<MenuOption>>? CachedMenu;

		public CustomizeUtil(Ktisis plugin) {
			Plugin = plugin;
			Data = plugin.DataManager;
		}

		// Calculate row index

		public uint GetMakeIndex(Customize custom) {
			var r = (uint)custom.Race;
			var t = (uint)custom.Tribe;
			var g = (uint)custom.Gender;
			var i = Customize.GetRaceTribeIndex(custom.Race);
			return ((r - 1) * 4) + ((t - i) * 2) + g; // Thanks cait
		}

		// Fetch char creator data from cache or sheet

		public CharaMakeType? GetMakeData(uint index) {
			if (Cached != null && Cached.RowId == index) {
				return Cached;
			} else {
				var lang = Plugin.Configuration.SheetLocale;
				var sheet = Data.GetExcelSheet<CharaMakeType>(lang);
				var row = sheet == null ? null : sheet.GetRow(index);
				Cached = row;
				return row;
			}
		}

		public CharaMakeType? GetMakeData(Customize custom) {
			var index = GetMakeIndex(custom);
			return GetMakeData(index);
		}

		// Build char creator options

		public Dictionary<MenuType, List<MenuOption>> GetMenuOptions(Customize custom) {
			var options = new Dictionary<MenuType, List<MenuOption>>();

			var index = GetMakeIndex(custom);
			if (Cached != null && CachedMenu != null && index == Cached.RowId)
				return CachedMenu;

			var data = GetMakeData(index);
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
								var icon = Data.GetImGuiTextureHqIcon(feat.Icon);
								if (feat.FeatureId == 0)
									continue;
								icons.Add(feat.FeatureId, icon!);
							}
						} else {
							for (var x = 0; x < val.Count; x++) {
								var icon = Data.GetImGuiTextureHqIcon(val.Params[x]);
								icons.Add(val.Graphics[x], icon!);
							}
						}
						opt.Select = icons;
					}

					options[type].Add(opt);
				}
			}

			CachedMenu = options;
			return options;
		}
	}

	public class CharaMakeIterator : IEnumerable {
		public const int Count = 28;

		public CharaMakeType Make;

		public CharaMakeIterator(CharaMakeType make) {
			Make = make;
		}

		public Menu GetMakeOption(int i) {
			return Make.Menus[i];
		}

		public Menu this[int index] {
			get => GetMakeOption(index);
			set => new NotImplementedException();
		}

		public IEnumerator GetEnumerator() {
			for (int i = 0; i < Count; i++)
				yield return this[i];
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