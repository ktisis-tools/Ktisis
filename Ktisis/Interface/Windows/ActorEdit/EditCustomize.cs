using System;
using System.Numerics;
using System.Collections.Generic;

using ImGuiNET;
using ImGuiScene;

using Dalamud.Logging;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Game.ClientState.Objects.Enums;

using Ktisis.GameData;
using Ktisis.Localization;
using Ktisis.GameData.Excel;
using Ktisis.Structs.Actor;
using Ktisis.Interface.Windows.ActorEdit;

namespace Ktisis.Interface.Windows {
	public struct MenuOption {
		public Menu Option;
		public Menu? Color = null;

		public Dictionary<uint, TextureWrap>? Select = null;

		public MenuOption(Menu option) => Option = option;
	}

	public static class EditCustomize {
		// Constants

		public static Vector2 IconSize = new(48, 48);
		public static Vector2 ListIconSize = new(58, 58);
		public static Vector2 IconPadding = new(8, 8);
		public static Vector2 InputSize = new(120, 120);
		public static Vector2 MiscInputSize = new(250, 250);

		// Properties

		public static bool Visible = false;

		public static CustomizeIndex? Selecting;
		public static Vector2 SelectPos;

		public static uint? CustomIndex = null;
		public static Dictionary<MenuType, List<MenuOption>> MenuOptions = new();

		public static int FaceType = -1;
		public static List<TextureWrap>? FacialFeatureIcons = null;

		public static CharaMakeType CharaMakeType = null!;

		public unsafe static Actor* Target => EditActor.Target;

		// Toggle visibility

		public static void Show() => Visible = true;

		// Apply customize

		public unsafe static void Apply(Customize custard) {
			if (Target != null) {
				var cur = Target->Customize;
				Target->Customize = custard;

				var tribeRedraw = cur.Race == Race.Hyur || cur.Race == Race.AuRa;
				if (cur.Race != custard.Race
					|| cur.Gender != custard.Gender
					|| cur.FaceType != custard.FaceType // Segfault at +31ACA4 and +31BA39
					|| (tribeRedraw && cur.Tribe != custard.Tribe)
				) {
					Target->Redraw();
				} else {
					Target->UpdateCustomize();
				}
			}
		}

		// Draw window

		public unsafe static void Draw() {
			// Customize

			var custom = Target->Customize;

			var index = custom.GetMakeIndex();
			if (index != CustomIndex) {
				MenuOptions = GetMenuOptions(index);
				CustomIndex = index;
				FacialFeatureIcons = null;
			}

			DrawFundamental(custom);
			DrawMenuType(custom, MenuType.Slider);
			DrawMenuType(custom, MenuType.List);
			DrawMenuType(custom, MenuType.Select);
			DrawFacialFeatures(custom);
		}

		public static void DrawMenuType(Customize custom, MenuType type) {
			var i = 0;
			foreach (var option in MenuOptions[type]) {
				switch (type) {
					case MenuType.Slider:
						DrawSlider(custom, option);
						break;
					case MenuType.SelectMulti:
						break;
					default:
						if (option.Option.HasIcon) {
							i++;
							if (i % 2 == 0) ImGui.SameLine();
						}
						DrawNumValue(custom, option);
						break;
				}
			}
			ImGui.Separator();
		}

		// Gender/Race/Tribe

		public static void DrawFundamental(Customize custom) {
			// Gender

			var isM = custom.Gender == Gender.Male;

			if (ImGuiComponents.IconButton(isM ? FontAwesomeIcon.Mars : FontAwesomeIcon.Venus)) {
				custom.Gender = isM ? Gender.Female : Gender.Male;
				Apply(custom);
			}

			ImGui.SameLine();
			ImGui.Text(isM ? "Masculine" : "Feminine");

			ImGui.BeginGroup();

			// TODO: Use Race and Tribe data from Lumina.

			ImGui.PushItemWidth(MiscInputSize.X);

			// Race

			var curRace = Locale.GetString($"{custom.Race}");
			if (ImGui.BeginCombo("Race", curRace)) {
				foreach (Race race in Enum.GetValues(typeof(Race))) {
					var raceName = Locale.GetString($"{race}");
					if (ImGui.Selectable(raceName, race == custom.Race)) {
						custom.Race = race;
						custom.Tribe = (Tribe)(
							custom.GetRaceTribeIndex()
							+ 1 - (byte)custom.Tribe % 2
						);
						Apply(custom);
					}
				}

				ImGui.SetItemDefaultFocus();
				ImGui.EndCombo();
			}

			// Tribe

			var curTribe = Locale.GetString($"{custom.Tribe}");
			if (ImGui.BeginCombo("Tribe", curTribe)) {
				for (int i = 0; i < 2; i++) {
					var tribe = (Tribe)(custom.GetRaceTribeIndex() + i);
					if (ImGui.Selectable(Locale.GetString($"{tribe}"), tribe == custom.Tribe)) {
						custom.Tribe = tribe;
						Apply(custom);
					}
				}

				ImGui.SetItemDefaultFocus();
				ImGui.EndCombo();
			}

			ImGui.PopItemWidth();

			ImGui.EndTabItem();
		}

		// Slider

		public unsafe static void DrawSlider(Customize custom, MenuOption option) {
			var opt = option.Option;
			var index = (int)opt.Index;
			var val = (int)custom.Bytes[index];

			ImGui.PushItemWidth(MiscInputSize.X);
			if (ImGui.SliderInt(opt.Name, ref val, 0, 100)) {
				custom.Bytes[index] = (byte)val;
				Apply(custom);
			}
			ImGui.PopItemWidth();
		}

		// Num values

		public unsafe static void DrawNumValue(Customize custom, MenuOption option) {
			var opt = option.Option;
			var index = (int)opt.Index;
			var val = (int)custom.Bytes[index];

			if (opt.HasIcon && option.Select != null) {
				DrawIconSelector(custom, option, (uint)val);
				ImGui.SameLine();
			}

			ImGui.BeginGroup();

			if (opt.HasIcon) ImGui.Text(opt.Name);
			ImGui.PushItemWidth(opt.HasIcon ? InputSize.X : MiscInputSize.X);
			if (ImGui.InputInt($"{(opt.HasIcon ? "##" : "")}{opt.Name}", ref val)) {
				custom.Bytes[index] = (byte)val;
				Apply(custom);
			}
			ImGui.PopItemWidth();

			ImGui.EndGroup();

			var col = option.Color;
			if (col != null) {
				// TODO
			}
		}

		// Icon selector

		public static void DrawIconSelector(Customize custom, MenuOption option, uint val) {
			var sel = option.Select;
			var size = IconSize;

			bool click;
			if (sel!.ContainsKey(val)) {
				click = ImGui.ImageButton(sel[val].ImGuiHandle, size);
			} else {
				click = ImGui.Button($"{val}", size + IconPadding);
			}

			var index = option.Option.Index;
			if (click) {
				Selecting = option.Option.Index;
				SelectPos = ImGui.GetMousePos();
				ImGui.SetNextWindowFocus();
			}

			if (Selecting == index)
				DrawIconList(custom, option);
		}

		public unsafe static void DrawIconList(Customize custom, MenuOption option) {
			var size = new Vector2(-1, -1);
			ImGui.SetNextWindowSize(size, ImGuiCond.Always);

			ImGui.SetNextWindowPos(SelectPos);
			ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(10, 10));

			var opt = option.Option;
			if (ImGui.Begin("Icon Select", ImGuiWindowFlags.NoDecoration)) {
				if (Selecting != null) {
					var focus = false;

					ImGui.BeginListBox("##feature_select", new Vector2(ListIconSize.X * 6 * 1.25f + 30, 200));
					focus |= ImGui.IsItemFocused() || ImGui.IsItemActive() || ImGui.IsItemActivated() || ImGui.IsItemHovered();

					int i = 0;
					foreach (var (val, icon) in option.Select!) {
						if (ImGui.ImageButton(icon.ImGuiHandle, ListIconSize)) {
							custom.Bytes[(uint)opt.Index] = (byte)val;
							Apply(custom);
						}
						focus |= ImGui.IsItemFocused();

						i++;
						if (i % 6 != 0)
							ImGui.SameLine(0f);
					}

					ImGui.EndListBox();

					focus |= ImGui.IsItemFocused();
					if (!focus && ImGui.IsItemHovered()) {
						ImGui.SetWindowFocus();
						focus = true;
					}

					if (!ImGui.IsWindowFocused() && !focus)
						Selecting = null;
				}

				ImGui.PopStyleVar(1);
				ImGui.End();
			}
		}

		// Facial feature selector

		public static void DrawFacialFeatures(Customize custom) {
			if (FacialFeatureIcons == null || custom.FaceType != FaceType) {
				var features = new List<TextureWrap>();
				for (var i = 0; i < 7; i++) {
					var index = custom.FaceType - 1 + (8 * i);
					var icon = Dalamud.DataManager.GetImGuiTextureIcon((uint)CharaMakeType.FacialFeatures[index]);
					features.Add(icon!);
				}
				FacialFeatureIcons = features;
				FaceType = custom.FaceType;
			}

			ImGui.BeginGroup();
			ImGui.PushItemWidth(InputSize.X - IconSize.X - IconPadding.X - 8);
			for (var i = 0; i < 8; i++) {
				if (i > 0 && i % 4 != 0)
					ImGui.SameLine();

				var value = (byte)Math.Pow(2, i);
				var isActive = (custom.FaceFeatures & value) != 0;

				bool button = false;
				ImGui.PushStyleColor(ImGuiCol.Button, (uint)(isActive ? 0x5F4F4FEF : 0x1FFFFFFF));
				if (i == 7) // Legacy tattoo
					button |= ImGui.Button("Legacy\nTattoo", IconSize + IconPadding);
				else
					button |= ImGui.ImageButton(FacialFeatureIcons[i].ImGuiHandle, IconSize);
				ImGui.PopStyleColor();

				if (button) {
					custom.FaceFeatures ^= value;
					Apply(custom);
				}
			}
			ImGui.PopItemWidth();
			ImGui.EndGroup();

			ImGui.SameLine();

			ImGui.BeginGroup();
			ImGui.Text("Facial Features");

			var input = (int)custom.FaceFeatures;
			ImGui.PushItemWidth(InputSize.X);
			if (ImGui.InputInt("##face_features", ref input)) {
				custom.FaceFeatures = (byte)input;
				Apply(custom);
			}

			ImGui.PopItemWidth();
			ImGui.EndGroup();
		}

		// Build menu options

		public static Dictionary<MenuType, List<MenuOption>> GetMenuOptions(uint index) {
			var options = new Dictionary<MenuType, List<MenuOption>>();

			var data = CharaMakeType;
			if (data == null || data.RowId != index) {
				CharaMakeType = Sheets.GetSheet<CharaMakeType>().GetRow(index)!;
				data = CharaMakeType;
			}

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
					if (type == MenuType.Color || type == MenuType.SelectMulti)
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
								var icon = Dalamud.DataManager.GetImGuiTextureIcon(feat.Icon);
								if (feat.FeatureId == 0)
									continue;
								icons.Add(feat.FeatureId, icon!);
							}
						} else {
							for (var x = 0; x < val.Count; x++) {
								var icon = Dalamud.DataManager.GetImGuiTextureIcon(val.Params[x]);
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
}