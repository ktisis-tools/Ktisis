using System;
using System.Linq;
using System.Numerics;
using System.Collections.Generic;

using ImGuiNET;

using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Interface.Textures;

using Ktisis.Util;
using Ktisis.Data;
using Ktisis.Data.Excel;
using Ktisis.Data.Files;
using Ktisis.Helpers.Async;
using Ktisis.Localization;
using Ktisis.Structs.Actor;
using Ktisis.Interface.Windows.ActorEdit;

using Lumina.Excel;

namespace Ktisis.Interface.Windows {
	public struct MenuOption {
		public Menu Option;
		public CustomizeIndex ColorIndex = 0;
		public uint[] Colors = Array.Empty<uint>();

		public Dictionary<uint, ISharedImmediateTexture>? Select = null;

		public MenuOption(Menu option) => Option = option;
	}

	public struct MenuColor {
		public string Name;
		public CustomizeIndex Index = 0;
		public CustomizeIndex AltIndex = 0;
		public uint[] Colors = Array.Empty<uint>();
		public uint[]? AltColors;
		public bool Iterable = true;

		public MenuColor(string name, CustomizeIndex index) {
			Name = name;
			Index = index;
		}
	}

	public static class EditCustomize {
		// Constants

		public static Vector2 IconSize => new(2 * ImGui.GetTextLineHeight() + ImGui.GetStyle().ItemSpacing.Y); // 48 <= these are the original values by Chirp
		public static Vector2 ListIconSize => new(3 * ImGui.GetTextLineHeight() + ImGui.GetStyle().ItemSpacing.Y); // 58
		public static Vector2 ButtonIconSize => IconSize + (ImGui.GetStyle().FramePadding * 2); // originally IconPadding = 8
		public static Vector2 InputSize => new(8 * ImGui.GetFontSize()); // 120
		public static Vector2 MiscInputSize => new(16 * ImGui.GetFontSize()); // 250
		public static Vector2 ColButtonSize => new Vector2(ImGui.GetTextLineHeight() + ImGui.GetStyle().ItemSpacing.Y) + (ImGui.GetStyle().FramePadding * 2); // 28
		public static Vector2 ColButtonSizeSmall => new(ImGui.GetTextLineHeight()); // 20

		// Properties

		public static bool Visible = false;

		public static CustomizeIndex? Selecting;
		public static Vector2 SelectPos;

		public static uint? CustomIndex = null;
		public static Dictionary<MenuType, List<MenuOption>> MenuOptions = new();
		public static List<MenuColor> MenuColors = new();

		public static int FaceType = -1;
		public static string FacialFeatureName = "";
		public static List<ISharedImmediateTexture>? FacialFeatureIcons = null;

		public static CharaMakeType? CharaMakeData;

		public static HumanCmp HumanCmp = new();

		public unsafe static Actor* Target => EditActor.Target;

		// Toggle visibility

		public static void Show() => Visible = true;

		public static bool IsPosing => Interop.Hooks.PoseHooks.PosingEnabled;

		// Apply customize
		
		public unsafe static void Apply(Customize custard) {
			if (Target != null)
				Target->ApplyCustomize(custard);
		}
		
		// Async

		private readonly static AsyncTask GetMakeData = new(InvokeMakeData);
		private static void InvokeMakeData(object[] args) {
			if (args[0] is not uint index || args[1] is not Customize custom)
				return;
			
			MenuOptions = GetMenuOptions(index, custom);
			CustomIndex = index;
			FacialFeatureIcons = null;
		}
		
		private static void GetFeatureIcons(Customize custom) {
			if (CharaMakeData == null) return;

			var data = CharaMakeData.Value;
			
			var features = new List<ISharedImmediateTexture>();
			for (var i = 0; i < 7; i++) {
				var index = custom.FaceType - 1 + (8 * i);
				if (custom.Race == Race.Hrothgar)
					index -= 4; // ???

				if (index < 0 || index >= data.FacialFeatures.Length)
					index = 8 * i;

				var iconId = (uint)data.FacialFeatures[index];
				if (iconId == 0)
					iconId = (uint)data.FacialFeatures[8 * i];

                var icon = Services.Textures.GetFromGameIcon(iconId);
				if (icon != null) features.Add(icon);
			}
			FacialFeatureIcons = features;
			FaceType = custom.FaceType;
		}

		// Draw window

		public unsafe static void Draw() {
			// Customize

			if (Target->ModelId != 0) {
				ImGui.Text("Target actor must be a human to edit customization data.");
				ImGui.Spacing();
				if (ImGui.Button("Turn Human")) {
					Target->ModelId = 0;
					Target->Redraw();
				}
				ImGui.EndTabItem();
				return;
			}

			var custom = Target->GetCustomize();

			if (custom.Race == 0 || custom.Tribe == 0) {
				custom.Race = Race.Hyur;
				custom.Tribe = Tribe.Highlander;
			}

			var index = custom.GetMakeIndex();
			if (index != CustomIndex && !GetMakeData.IsRunning)
				GetMakeData.Run(index, custom);

			DrawFundamental(custom);
			DrawMenuType(custom, MenuType.Slider);
			ImGui.Separator();
			DrawCheckboxes(custom);
			DrawMenuType(custom, MenuType.List);
			ImGui.Separator();
			DrawColors(custom);
			ImGui.Separator();
			DrawMenuType(custom, MenuType.Select);
			ImGui.Separator();
			DrawFacialFeatures(custom);
		}

		// Gender/Race/Tribe

		public static void DrawFundamental(Customize custom) {
			// Gender

			var isM = custom.Gender == Gender.Masculine;
			if (ImGuiComponents.IconButton("##KtisisCustomizeGender", isM ? FontAwesomeIcon.Mars : FontAwesomeIcon.Venus)) {
				custom.Gender = isM ? Gender.Feminine : Gender.Masculine;
				Apply(custom);
			}

			ImGui.SameLine();
			ImGui.Text(isM ? "Masculine" : "Feminine");

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

		// Draw MenuType

		public static void DrawMenuType(Customize custom, MenuType type) {
			if (!MenuOptions.ContainsKey(type)) return;

			var i = 0;
			foreach (var option in MenuOptions[type]) {
				switch (type) {
					case MenuType.Slider:
						DrawSlider(custom, option);
						break;
					default:
						if (option.Option.Index == CustomizeIndex.EyeColor2)
							continue;
						if (option.Option.HasIcon) {
							i++;
							if (i % 2 == 0) ImGui.SameLine();
						}
						DrawNumValue(custom, option);
						break;
				}
			}
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

		// Checkbox options

		public static void DrawCheckboxes(Customize custom) {
			var highlights = custom.HasHighlights == 0x80;
			if (ImGui.Checkbox("Highlights", ref highlights)) {
				custom.HasHighlights ^= 0x80;
				Apply(custom);
			}

			var flipPaint = ((uint)custom.Facepaint & 0x80) > 0;
			ImGui.SameLine();
			if (ImGui.Checkbox("Flip Facepaint", ref flipPaint)) {
				custom.Facepaint ^= (FacialFeature)0x80;
				Apply(custom);
			}

			var smallIris = (custom.EyeShape & 0x80) > 0;
			ImGui.SameLine();
			if (ImGui.Checkbox("Small Iris", ref smallIris)) {
				custom.EyeShape ^= 0x80;
				Apply(custom);
			}

			if (custom.Race != Race.Hrothgar) {
				var lipCol = (custom.LipStyle & 0x80) > 0;
				ImGui.SameLine();
				if (ImGui.Checkbox("Lip Color", ref lipCol)) {
					custom.LipStyle ^= 0x80;
					Apply(custom);
				}
			}
		}

		// Num values

		public unsafe static void DrawNumValue(Customize custom, MenuOption option) {
			var opt = option.Option;
			var index = (int)opt.Index;
			var val = (int)custom.Bytes[index];

			if (opt.HasIcon && option.Select != null) {
				DrawIconSelector(custom, option, val);
				ImGui.SameLine();
			}

			// TODO: fix FramePadding Y not rewinded on SameLine under some conditions
			ImGui.BeginGroup();

			if (opt.HasIcon) ImGui.Text(opt.Name);
			ImGui.PushItemWidth(opt.HasIcon ? InputSize.X : MiscInputSize.X);
			if (ImGui.InputInt($"{(opt.HasIcon ? "##" : "")}{opt.Name}", ref val)) {
				custom.Bytes[index] = (byte)val;
				Apply(custom);
			}
			ImGui.PopItemWidth();

			ImGui.EndGroup();
		}

		// Icon selector

		public static void DrawIconSelector(Customize custom, MenuOption option, int _val) {
			var sel = option.Select;

			var val = (uint)_val;
			if (option.Option.Index == CustomizeIndex.Facepaint)
				val = (uint)(val & ~0x80);

			bool click;
			if (sel!.ContainsKey(val))
				click = ImGui.ImageButton(sel[val].GetWrapOrEmpty().ImGuiHandle, IconSize);
			else
				click = ImGui.Button($"{val}", ButtonIconSize);

			var index = option.Option.Index;
			if (click) {
				Selecting = option.Option.Index;
				SelectPos = new Vector2(ImGui.GetItemRectMax().X + 5, ImGui.GetItemRectMin().Y);
				ImGui.SetNextWindowFocus();
			}

			if (Selecting == index)
				DrawIconList(custom, option);
		}

		// Color selection

		public static void DrawColors(Customize custom) {
			if (MenuColors.Count == 0) return;
			var colors = MenuColors.OrderBy(c => c.AltIndex);

			var i = 0;
			foreach (var color in colors) {
				if (!color.Iterable) continue;

				if (color.AltIndex != 0 && i > -1) i = -1;
				if (i != -1) {
					if (i % 4 != 0) ImGui.SameLine();
					i++;
				}

				DrawColor(custom, color);
			}
		}

		public unsafe static void DrawColor(Customize custom, MenuColor color) {
			var colIndex = custom.Bytes[(uint)color.Index];
			var colRgb = colIndex >= color.Colors.Length || colIndex < 0 ? 0 : color.Colors[colIndex];

			CustomizeIndex selecting = 0;

			ImGui.BeginGroup();

			if (DrawColorButton($"{colIndex}##{color.Name}", colRgb))
				selecting = color.Index;

			if (color.AltIndex != 0) {
				var altColor = color.AltColors ?? color.Colors;
				var altIndex = custom.Bytes[(uint)color.AltIndex];
				var altRgb = altIndex >= color.Colors.Length ? 0 : altColor[altIndex];
				ImGui.SameLine();
				if (DrawColorButton($"{altIndex}##{color.Name}##alt", altRgb))
					selecting = color.AltIndex;
			}

			if (selecting != 0) {
				Selecting = selecting;
				SelectPos = new Vector2(ImGui.GetItemRectMax().X + 5, ImGui.GetItemRectMin().Y);
				ImGui.SetNextWindowFocus();
			}

			ImGui.SameLine();

			var name = color.Name;
			if (color.AltIndex != 0) name += "s";
			ImGui.Text(name);

			if (Selecting == color.Index || Selecting == color.AltIndex) {
				byte value = custom.Bytes[(uint)Selecting];
				var colors = Selecting == color.AltIndex ? (color.AltColors ?? color.Colors) : color.Colors;
				if (DrawColorList(colors, ref value)) {
					custom.Bytes[(uint)Selecting] = value;
					Apply(custom);
				}
			}

			ImGui.EndGroup();
		}

		public static bool DrawColorButton(string name, uint color) {
			var textCol = GuiHelpers.CalcContrastRatio(0xffffffff, color) < 1.5 ? 0xff000000 : 0xffffffff;
			ImGui.PushStyleColor(ImGuiCol.Text, textCol);
			ImGui.PushStyleColor(ImGuiCol.Button, color);
			ImGui.PushStyleColor(ImGuiCol.ButtonHovered, color);

			var result = ImGui.Button(name, ColButtonSize);

			ImGui.PopStyleColor(3);

			return result;
		}

		public static bool DrawColorList(uint[] colors, ref byte value) {
			var result = false;

			var size = new Vector2(-1, -1);
			ImGui.SetNextWindowSize(size, ImGuiCond.Always);

			ImGui.SetNextWindowPos(SelectPos);
			ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(10, 10));

			if (ImGui.Begin("Color Select", ImGuiWindowFlags.NoDecoration)) {
				if (Selecting != null) {
					var focus = false;

					//ImGui.BeginListBox("##feature_select", new Vector2(ListIconSize.X * 6 * 1.25f + 30, 200));
					//focus |= ImGui.IsItemFocused() || ImGui.IsItemActive() || ImGui.IsItemActivated() || ImGui.IsItemHovered();

					var id = (int)value;
					ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - GuiHelpers.WidthMargin());
					if (ImGui.InputInt("##colId", ref id)) {
						value = (byte)id;
						result |= true;
					}

					ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(0,0));
					ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 0);
					for (var i = 0; i < colors.Length; i++) {
						var rgb = colors[i];
						if (rgb == 0) continue;

						if (i % 8 != 0) ImGui.SameLine();

						var rgba = ImGui.ColorConvertU32ToFloat4(rgb);
						if (ImGui.ColorButton($"{i}##col_select_{i}", rgba, ImGuiColorEditFlags.NoBorder, ColButtonSizeSmall)) {
							result |= true;
							value = (byte)i;
							ImGui.SetWindowFocus();
						}
					}
					ImGui.PopStyleVar(2);

					//ImGui.EndListBox();

					focus |= ImGui.IsItemFocused();
					if (!focus && ImGui.IsItemHovered()) {
						ImGui.SetWindowFocus();
						focus = true;
					}

					if (!ImGui.IsWindowFocused() && !focus)
						Selecting = null;
				}

				var maxPos = SelectPos + ImGui.GetWindowContentRegionMax();
				var displaySize = ImGui.GetIO().DisplaySize;
				if (maxPos.X > displaySize.X || maxPos.Y > displaySize.Y) {
					SelectPos = new Vector2(
						Math.Min(SelectPos.X, SelectPos.X - (maxPos.X - displaySize.X)),
						Math.Min(SelectPos.Y, SelectPos.Y - (maxPos.Y - displaySize.Y))
					);
				}

				ImGui.PopStyleVar(1);
				ImGui.End();
			}

			return result;
		}

		// Facial feature selector

		public static void DrawFacialFeatures(Customize custom) {
			if ((FacialFeatureIcons == null || custom.FaceType != FaceType))
				GetFeatureIcons(custom);

			if (FacialFeatureIcons == null) return;

			ImGui.BeginGroup();
			ImGui.PushItemWidth(InputSize.X - ButtonIconSize.X);
			for (var i = 0; i < 8; i++) {
				if (i < 7 && i >= FacialFeatureIcons.Count)
					break;

				if (i > 0 && i % 4 != 0)
					ImGui.SameLine();

				var value = (byte)Math.Pow(2, i);
				var isActive = (custom.FaceFeatures & value) != 0;

				bool button = false;
				ImGui.PushStyleColor(ImGuiCol.Button, isActive ? 0x5F4F4FEFu : 0x1FFFFFFFu);
				if (i == 7) // Legacy tattoo
					button |= ImGui.Button("Legacy\nTattoo", ButtonIconSize);
				else
					button |= ImGui.ImageButton(FacialFeatureIcons[i].GetWrapOrEmpty().ImGuiHandle, IconSize);
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
			ImGui.Text(FacialFeatureName);
			var input = (int)custom.FaceFeatures;
			ImGui.PushItemWidth(InputSize.X);
			if (ImGui.InputInt("##face_features", ref input)) {
				custom.FaceFeatures = (byte)input;
				Apply(custom);
			}

			ImGui.Separator();


			foreach (var color in MenuColors) {
				if (color.Index != CustomizeIndex.FaceFeaturesColor) continue;
				DrawColor(custom, color);
			}

			ImGui.PopItemWidth();
			ImGui.EndGroup();
		}

		// Icon selection

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
						if (icon == null) continue;

						if (ImGui.ImageButton(icon.GetWrapOrEmpty().ImGuiHandle, ListIconSize)) {
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

		// Build menu options

		public static Dictionary<MenuType, List<MenuOption>> GetMenuOptions(uint index, Customize custom) {
			var options = new Dictionary<MenuType, List<MenuOption>>();
			
			if (CharaMakeData == null || CharaMakeData.Value.RowId != index) {
				CharaMakeData = Sheets.GetSheet<CharaMakeType>().GetRow(index);
				MenuColors.Clear();
			}
			
			var data = CharaMakeData!.Value;
			
			for (int i = 0; i < CharaMakeType.MenuCt; i++) {
				var val = data.Menus[i];

				if (val.Index == 0)
					break;

				var type = val.Type;
				if (type == MenuType.Unknown1)
					type = MenuType.Color;

				if (type == MenuType.Color) { // I gave up on making this work procedurally
					var menuCol = new MenuColor(val.Name, val.Index);
					switch (val.Index) {
						case CustomizeIndex.EyeColor:
							menuCol.Colors = HumanCmp.GetEyeColors();
							menuCol.AltIndex = CustomizeIndex.EyeColor2;
							break;
						case CustomizeIndex.FaceFeaturesColor:
							menuCol.Colors = HumanCmp.GetEyeColors();
							menuCol.Iterable = false;
							break;
						case CustomizeIndex.FacepaintColor:
							menuCol.Colors = HumanCmp.GetFacepaintColors();
							break;
						case CustomizeIndex.HairColor:
							menuCol.Colors = HumanCmp.GetHairColors(data.TribeEnum, data.GenderEnum);
							menuCol.AltIndex = CustomizeIndex.HairColor2;
							menuCol.AltColors = HumanCmp.GetHairHighlights();
							break;
						case CustomizeIndex.LipColor:
							menuCol.Colors = HumanCmp.GetLipColors();
							break;
						case CustomizeIndex.SkinColor:
							menuCol.Colors = HumanCmp.GetSkinColors(data.TribeEnum, data.GenderEnum);
							break;
						default:
							Logger.Warning($"Color not implemented: {val.Index}");
							break;
					}
					MenuColors.Add(menuCol);
					continue;
				}

				if (type == MenuType.SelectMulti) {
					if (val.Index == CustomizeIndex.FaceFeatures)
						FacialFeatureName = val.Name;
					continue;
				}

				if (!options.ContainsKey(type))
					options[type] = new();

				var opt = new MenuOption(val);

				if (val.HasIcon) {
					var icons = new Dictionary<uint, ISharedImmediateTexture>();
					if (val.IsFeature) {
						var featMake = CharaMakeData?.FeatureMake.Value;
						if (featMake == null) continue;

						List<RowRef<CharaMakeCustomize>> features;
						if (val.Index == CustomizeIndex.HairStyle)
							features = featMake.Value.HairStyles;
						else if (val.Index == CustomizeIndex.Facepaint)
							features = featMake.Value.Facepaints;
						else continue;

						foreach (var feature in features) {
							var feat = feature.Value;
							if (feat.FeatureId == 0) break;

							if (!Services.Textures.TryGetFromGameIcon(feat.Icon, out var icon))
								continue;

							icons.Add(feat.FeatureId, icon);
						}
					} else {
						for (var x = 0; x < val.Count; x++) {
							if (!Services.Textures.TryGetFromGameIcon(val.Params[x], out var icon))
								continue;
							
							icons.Add(val.Graphics[x], icon);
						}
					}
					opt.Select = icons;
				}

				options[type].Add(opt);
			}

			return options;
		}
	}
}
