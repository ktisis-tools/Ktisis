using System;
using System.Numerics;
using System.Collections.Generic;

using ImGuiNET;

using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.ClientState.Objects.Enums;

using Ktisis.Util;
using Ktisis.Localization;
using Ktisis.GameData.Excel;
using Ktisis.Structs.Actor;

namespace Ktisis.Interface.Windows {
	internal class CustomizeGui {
		// Constants

		public const int IconSize = 54;
		public const int IconPadding = 8;
		public const int InputSize = 260;

		// Properties

		public static bool Visible = false;

		public static CustomizeIndex? Selecting;
		public static Vector2 SelectPos;

		public static uint? CustomIndex = null;
		public static Dictionary<MenuType, List<MenuOption>> MenuOptions = new();

		public unsafe static Actor* Target
			=> Ktisis.GPoseTarget != null ? (Actor*)Ktisis.GPoseTarget.Address : null;

		// Toggle visibility

		public static void Show() => Visible = true;

		// Apply customize

		public unsafe static void Apply(Customize custard) {
			if (Target != null) {
				Target->Customize = custard;
				Target->Redraw();
			}
		}

		// Draw window

		public unsafe static void Draw() {
			if (!Visible)
				return;

			if (Target == null)
				return;

			var size = new Vector2(400, -1);
			ImGui.SetNextWindowSize(size, ImGuiCond.Always);

			ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(10, 10));

			// Create window
			var title = Ktisis.Configuration.DisplayCharName ? $"{Target->Name}" : "Appearance";
			if (ImGui.Begin(title, ref Visible, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoResize)) {
				ImGui.BeginGroup();
				ImGui.AlignTextToFramePadding();

				// Customize

				var custom = Target->Customize;

				DrawFundamental(custom);

				var index = CustomizeUtil.GetMakeIndex(custom);
				if (index != CustomIndex) {
					MenuOptions = CustomizeUtil.GetMenuOptions(index);
					CustomIndex = index;
				}

				foreach (var type in MenuOptions.Keys) {
					ImGui.Separator();
					foreach (var option in MenuOptions[type]) {
						switch (type) {
							case MenuType.Slider:
								DrawSlider(custom, option);
								break;
							default:
								DrawNumValue(custom, option);
								break;
						}
					}
				}

				// End

				ImGui.PopStyleVar(1);
				ImGui.End();
			}
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

			// TODO: Use Race and Tribe data from Lumina.

			// Race

			var curRace = Locale.GetString($"{custom.Race}");
			if (ImGui.BeginCombo("Race", curRace)) {
				foreach (Race race in Enum.GetValues(typeof(Race))) {
					var raceName = Locale.GetString($"{race}");
					if (ImGui.Selectable(raceName, race == custom.Race)) {
						custom.Race = race;
						custom.Tribe = (Tribe)(
							Customize.GetRaceTribeIndex(race)
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
					var tribe = (Tribe)(Customize.GetRaceTribeIndex(custom.Race) + i);
					if (ImGui.Selectable(Locale.GetString($"{tribe}"), tribe == custom.Tribe)) {
						custom.Tribe = tribe;
						Apply(custom);
					}
				}

				ImGui.SetItemDefaultFocus();
				ImGui.EndCombo();
			}
		}

		// Slider

		public unsafe static void DrawSlider(Customize custom, MenuOption option) {
			var opt = option.Option;
			var index = (int)opt.Index;
			var val = (int)custom.Bytes[index];

			if (ImGui.SliderInt(opt.Name, ref val, 0, 100)) {
				custom.Bytes[index] = (byte)val;
				Apply(custom);
			}
		}

		// Num values

		public unsafe static void DrawNumValue(Customize custom, MenuOption option) {
			var opt = option.Option;
			var index = (int)opt.Index;
			var val = (int)custom.Bytes[index];

			if (opt.HasIcon && option.Select != null) {
				DrawIconSelector(custom, option, (uint)val);
				ImGui.SameLine();
				ImGui.PushItemWidth(InputSize - IconSize - IconPadding - 8);
			} else ImGui.PushItemWidth(InputSize);

			if (ImGui.InputInt(opt.Name, ref val)) {
				custom.Bytes[index] = (byte)val;
				Apply(custom);
			}
			ImGui.PopItemWidth();

			var col = option.Color;
			if (col != null) {
				// TODO
			}
		}

		// Icon selector

		public static void DrawIconSelector(Customize custom, MenuOption option, uint val) {
			var sel = option.Select;
			var size = new Vector2(IconSize, IconSize);

			bool click;
			if (sel!.ContainsKey(val)) {
				click = ImGui.ImageButton(sel[val].ImGuiHandle, size);
			} else {
				size.X += IconPadding;
				size.Y += IconPadding;
				click = ImGui.Button($"{val}", size);
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
				if (ImGui.IsWindowFocused()) {
					int i = 0;
					foreach (var (val, icon) in option.Select!) {
						if (ImGui.ImageButton(icon.ImGuiHandle, new Vector2(IconSize, IconSize))) {
							custom.Bytes[(uint)opt.Index] = (byte)val;
							Apply(custom);
						}

						i++;
						if (i % 6 != 0)
							ImGui.SameLine();
					}
				} else {
					Selecting = null;
				}

				ImGui.PopStyleVar(1);
				ImGui.End();
			}
		}
	}
}