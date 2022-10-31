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

			if (ImGui.Begin("Ktisis Settings", ref Visible, ImGuiWindowFlags.NoResize)) {
				if (ImGui.BeginTabBar("Settings")) {
					var cfg = Ktisis.Configuration;
					if (ImGui.BeginTabItem("Interface"))
						DrawInterfaceTab(cfg);
					if (ImGui.BeginTabItem("Overlay"))
						DrawOverlayTab(cfg);
					if (ImGui.BeginTabItem("Gizmo"))
						DrawGizmoTab(cfg);
					if (ImGui.BeginTabItem("Language"))
						DrawLanguageTab(cfg);

					ImGui.EndTabBar();
				}
			}

			ImGui.PopStyleVar(1);
			ImGui.End();
		}

		// Interface

		public static void DrawInterfaceTab(Configuration cfg) {
			var displayCharName = !cfg.DisplayCharName;
			if (ImGui.Checkbox("Hide character name", ref displayCharName))
				cfg.DisplayCharName = !displayCharName;

			ImGui.EndTabItem();
		}

		// Overlay

		public static void DrawOverlayTab(Configuration cfg) {
			var drawLines = cfg.DrawLinesOnSkeleton;
			if (ImGui.Checkbox("Draw lines on skeleton", ref drawLines))
				cfg.DrawLinesOnSkeleton = drawLines;

			var lineThickness = cfg.SkeletonLineThickness;
			if (ImGui.SliderFloat("Lines thickness", ref lineThickness, 0.01F, 15F, "%.1f"))
				cfg.SkeletonLineThickness = lineThickness;

			ImGui.Separator();
			ImGui.Text("Bone colors");

			bool linkBoneCategoriesColors = cfg.LinkBoneCategoryColors;
			if (GuiHelpers.IconButtonTooltip(cfg.LinkBoneCategoryColors ? FontAwesomeIcon.Link : FontAwesomeIcon.Unlink, linkBoneCategoriesColors ? "Unlink bones colors" : "Link bones colors"))
				cfg.LinkBoneCategoryColors = !linkBoneCategoriesColors;

			ImGui.SameLine();
			if (GuiHelpers.IconButtonHoldConfirm(FontAwesomeIcon.Eraser, "Hold Control and Shift to erase colors.", ImGui.GetIO().KeyCtrl && ImGui.GetIO().KeyShift))
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
				if (ImGui.ColorEdit4("Bones color", ref linkedBoneColor, ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.AlphaBar))
					cfg.LinkedBoneCategoryColor = linkedBoneColor;
			} else {

				ImGui.SameLine();
				if (GuiHelpers.IconButtonHoldConfirm(FontAwesomeIcon.Rainbow, "Hold Control and Shift to reset colors to their default values.", ImGui.GetIO().KeyCtrl && ImGui.GetIO().KeyShift))
				{
					foreach ((string categoryName, Category category) in Category.Categories)
					{
						if (!category.ShouldDisplay && !cfg.BoneCategoryColors.ContainsKey(category.Name))
							continue;
						cfg.BoneCategoryColors[category.Name] = category.DefaultColor;
					}
				}

				ImGui.Text("Categories colors:");
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
					ImGui.TextWrapped("Categories will be added after bones are displayed once.");
			}

			ImGui.EndTabItem();
		}

		// Gizmo

		public static void DrawGizmoTab(Configuration cfg) {
			var allowAxisFlip = cfg.AllowAxisFlip;
			if (ImGui.Checkbox("Flip axis to face camera", ref allowAxisFlip))
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

			if (ImGui.BeginCombo("Language", selected)) {
				foreach (var lang in Locale.Languages) {
					var name = $"{lang}";
					if (ImGui.Selectable(name, name == selected))
						cfg.Localization = lang;
				}

				ImGui.SetItemDefaultFocus();
				ImGui.EndCombo();
			}

			var translateBones = cfg.TranslateBones;
			if (ImGui.Checkbox("Translate bone names", ref translateBones))
				cfg.TranslateBones = translateBones;

			ImGui.EndTabItem();
		}
	}
}
