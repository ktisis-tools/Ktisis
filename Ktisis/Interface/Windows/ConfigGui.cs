using System.Numerics;

using ImGuiNET;

using Dalamud.Interface.Components;
using Dalamud.Interface;

using Ktisis.Util;
using Ktisis.Localization;
using Ktisis.Structs.Bones;

namespace Ktisis.Interface.Windows {
	internal static class ConfigGui {
		public static bool Visible = false;

		// Toggle visibility

		public static void Show() {
			Visible = true;
		}

		public static void Hide() {
			Visible = false;
		}

		// Draw

		public static void Draw() {
			if (!Visible)
				return;

			var size = new Vector2(-1, -1);
			ImGui.SetNextWindowSize(size, ImGuiCond.Always);
			ImGui.SetNextWindowSizeConstraints(size, size);

			ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(10, 10));

			if (ImGui.Begin(Locale.GetString("Ktisis_settings"), ref Visible, ImGuiWindowFlags.NoResize)) {
				if (ImGui.BeginTabBar(Locale.GetString("Settings"))) {
					var cfg = Ktisis.Configuration;
					if (ImGui.BeginTabItem(Locale.GetString("Interface")))
						DrawInterfaceTab(cfg);
					if (ImGui.BeginTabItem(Locale.GetString("Overlay")))
						DrawOverlayTab(cfg);
					if (ImGui.BeginTabItem(Locale.GetString("Gizmo")))
						DrawGizmoTab(cfg);
					if (ImGui.BeginTabItem(Locale.GetString("Language")))
						DrawLanguageTab(cfg);

					ImGui.EndTabBar();
				}
			}

			ImGui.PopStyleVar(1);
			ImGui.End();
		}

		// Interface

		public static void DrawInterfaceTab(Configuration cfg) {

			ImGui.Text(Locale.GetString("General"));

			var displayCharName = !cfg.DisplayCharName;
			if (ImGui.Checkbox(Locale.GetString("Hide_char_name"), ref displayCharName))
				cfg.DisplayCharName = !displayCharName;

			ImGui.Spacing();
			ImGui.Separator();
			ImGui.Text(Locale.GetString("Transform_table"));

			ImGui.PushItemWidth(ImGui.GetFontSize() * 4);
			var transformTablePointDigits = cfg.TransformTableDigitPrecision;
			if (ImGui.DragInt(Locale.GetString("Digit_precision"), ref transformTablePointDigits, 1f, 1, 8))
				cfg.TransformTableDigitPrecision = transformTablePointDigits;

			var transformTableBaseSpeedPos = cfg.TransformTableBaseSpeedPos;
			if (ImGui.DragFloat(Locale.GetString("Base_position_speed"), ref transformTableBaseSpeedPos, 1f, 0.00001f, 10000f, "%.4f", ImGuiSliderFlags.Logarithmic))
				cfg.TransformTableBaseSpeedPos = transformTableBaseSpeedPos;

			var transformTableBaseSpeedRot = cfg.TransformTableBaseSpeedRot;
			if (ImGui.DragFloat(Locale.GetString("Base_rotation_speed"), ref transformTableBaseSpeedRot, 1f, 0.00001f, 10000f, "%.4f", ImGuiSliderFlags.Logarithmic))
				cfg.TransformTableBaseSpeedRot = transformTableBaseSpeedRot;

			var transformTableBaseSpeedSca = cfg.TransformTableBaseSpeedSca;
			if (ImGui.DragFloat(Locale.GetString("Base_scale_speed"), ref transformTableBaseSpeedSca, 1f, 0.00001f, 10000f, "%.4f", ImGuiSliderFlags.Logarithmic))
				cfg.TransformTableBaseSpeedSca = transformTableBaseSpeedSca;

			var transformTableModifierMultCtrl = cfg.TransformTableModifierMultCtrl;
			if (ImGui.DragFloat(Locale.GetString("Ctrl_speed_multiplier"), ref transformTableModifierMultCtrl, 1f, 0.00001f, 10000f, "%.4f",ImGuiSliderFlags.Logarithmic))
				cfg.TransformTableModifierMultCtrl = transformTableModifierMultCtrl;

			var transformTableModifierMultShift = cfg.TransformTableModifierMultShift;
			if (ImGui.DragFloat(Locale.GetString("Shift_speed_multiplier"), ref transformTableModifierMultShift, 1f, 0.00001f, 10000f, "%.4f", ImGuiSliderFlags.Logarithmic))
				cfg.TransformTableModifierMultShift = transformTableModifierMultShift;

			var displayMultiplierInputs = cfg.TransformTableDisplayMultiplierInputs;
			if (ImGui.Checkbox(Locale.GetString("Show_speed_multipler_inputs"), ref displayMultiplierInputs))
				cfg.TransformTableDisplayMultiplierInputs = displayMultiplierInputs;
			ImGui.PopItemWidth();

			ImGui.EndTabItem();
		}

		// Overlay

		public static void DrawOverlayTab(Configuration cfg) {
			var drawLines = cfg.DrawLinesOnSkeleton;
			if (ImGui.Checkbox(Locale.GetString("Draw_lines_on_skeleton"), ref drawLines))
				cfg.DrawLinesOnSkeleton = drawLines;

			var lineThickness = cfg.SkeletonLineThickness;
			if (ImGui.SliderFloat(Locale.GetString("Lines_thickness"), ref lineThickness, 0.01F, 15F, "%.1f"))
				cfg.SkeletonLineThickness = lineThickness;

			ImGui.Separator();
			ImGui.Text(Locale.GetString("Bone_colors"));

			bool linkBoneCategoriesColors = cfg.LinkBoneCategoryColors;
			if (GuiHelpers.IconButtonTooltip(cfg.LinkBoneCategoryColors ? FontAwesomeIcon.Link : FontAwesomeIcon.Unlink, linkBoneCategoriesColors ? Locale.GetString("Unlink_bones_colors") : Locale.GetString("Link_bones_colors")))
				cfg.LinkBoneCategoryColors = !linkBoneCategoriesColors;

			ImGui.SameLine();
			if (GuiHelpers.IconButtonHoldConfirm(FontAwesomeIcon.Eraser, Locale.GetString("Hold_Control_and_Shift_to_erase_colors"), ImGui.GetIO().KeyCtrl && ImGui.GetIO().KeyShift))
			{
				Vector4 eraseColor = new(1.0F, 1.0F, 1.0F, 0.5647059F);
				if (linkBoneCategoriesColors) {
					cfg.LinkedBoneCategoryColor = eraseColor;
				} else {
					foreach (Category category in Category.Categories.Values) {
						if (category.ShouldDisplay || cfg.BoneCategoryColors.ContainsKey(category.Name))
							cfg.BoneCategoryColors[category.Name] = eraseColor;
					}
				}
			}

			if (linkBoneCategoriesColors)
			{
				Vector4 linkedBoneColor = cfg.LinkedBoneCategoryColor;
				if (ImGui.ColorEdit4(Locale.GetString("Bone_colors"), ref linkedBoneColor, ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.AlphaBar))
					cfg.LinkedBoneCategoryColor = linkedBoneColor;
			} else {

				ImGui.SameLine();
				if (GuiHelpers.IconButtonHoldConfirm(FontAwesomeIcon.Rainbow, "Hold_Control_and_Shift_to_reset_colors_to_their_default_values", ImGui.GetIO().KeyCtrl && ImGui.GetIO().KeyShift))
				{
					foreach ((string categoryName, Category category) in Category.Categories)
					{
						if (!category.ShouldDisplay && !cfg.BoneCategoryColors.ContainsKey(category.Name))
							continue;
						cfg.BoneCategoryColors[category.Name] = category.DefaultColor;
					}
				}

				ImGui.Text(Locale.GetString("Categories_colors"));
				ImGui.Columns(2);
				int i = 0;
				bool hasShownAnyCategory = false;
				foreach (Category category in Category.Categories.Values) {
					if (!category.ShouldDisplay && !cfg.BoneCategoryColors.ContainsKey(category.Name))
						continue;

					if (!cfg.BoneCategoryColors.TryGetValue(category.Name, out Vector4 categoryColor))
						categoryColor = cfg.LinkedBoneCategoryColor;

					if (ImGui.ColorEdit4(category.Name, ref categoryColor, ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.AlphaBar))
						cfg.BoneCategoryColors[category.Name] = categoryColor;

					if (i % 2 != 0) ImGui.NextColumn();
					i++;
					hasShownAnyCategory = true;
				}
				ImGui.Columns();
				if (!hasShownAnyCategory)
					ImGui.TextWrapped(Locale.GetString("Categories_will_be_added_after_bones_are_displayed_once"));
			}

			ImGui.EndTabItem();
		}

		// Gizmo

		public static void DrawGizmoTab(Configuration cfg) {
			var allowAxisFlip = cfg.AllowAxisFlip;
			if (ImGui.Checkbox(Locale.GetString("Flip_axis_to_face_camera"), ref allowAxisFlip))
				cfg.AllowAxisFlip = allowAxisFlip;

			ImGui.EndTabItem();
		}

		// Language

		public static void DrawLanguageTab(Configuration cfg) {
			var selected = "";
			foreach (var lang in Locale.Languages) {
				if (lang == cfg.Localization) {
					selected = $"{lang}";
					break;
				}
			}

			if (ImGui.BeginCombo(Locale.GetString("Language"), selected)) {
				foreach (var lang in Locale.Languages) {
					var name = $"{lang}";
					if (ImGui.Selectable(name, name == selected))
						cfg.Localization = lang;
				}

				ImGui.SetItemDefaultFocus();
				ImGui.EndCombo();
			}

			var translateBones = cfg.TranslateBones;
			if (ImGui.Checkbox(Locale.GetString("Translate_bone_names"), ref translateBones))
				cfg.TranslateBones = translateBones;

			ImGui.EndTabItem();
		}
	}
}
