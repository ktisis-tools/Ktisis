using System.Numerics;
using System.Collections.Generic;

using ImGuiNET;

using Dalamud.Interface.Components;
using Dalamud.Interface;

using Ktisis.Localization;
using Ktisis.Structs.Bones;

namespace Ktisis.Interface {
	internal class ConfigGui {
		private Ktisis Plugin;

		private Configuration Cfg;

		public bool Visible = false;

		// Constructor

		public ConfigGui(Ktisis plugin) {
			Plugin = plugin;
			Cfg = Plugin.Configuration;
		}

		// Toggle visibility

		public void Show() {
			Visible = true;
		}

		public void Hide() {
			Visible = false;
		}

		// Draw

		public void Draw() {
			if (!Visible)
				return;

			var size = new Vector2(-1, -1);
			ImGui.SetNextWindowSize(size, ImGuiCond.Always);
			ImGui.SetNextWindowSizeConstraints(size, size);

			ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(10, 10));

			if (ImGui.Begin("Ktisis Settings", ref Visible, ImGuiWindowFlags.NoResize)) {
				if (ImGui.BeginTabBar("Settings")) {
					if (ImGui.BeginTabItem("Interface"))
						DrawInterfaceTab();
					if (ImGui.BeginTabItem("Overlay"))
						DrawOverlayTab();
					if (ImGui.BeginTabItem("Gizmo"))
						DrawGizmoTab();
					if (ImGui.BeginTabItem("Language"))
						DrawLanguageTab();

					ImGui.EndTabBar();
				}
			}

			ImGui.PopStyleVar(1);
			ImGui.End();
		}

		// Interface

		public void DrawInterfaceTab() {
			/*var autoOpen = cfg.AutoOpen;
			if (ImGui.Checkbox("Auto Open", ref autoOpen)) {
				cfg.AutoOpen = autoOpen;
				cfg.Save(Plugin);
			}*/

			var displayCharName = Cfg.DisplayCharName;
			if (ImGui.Checkbox("Display character name", ref displayCharName)) {
				Cfg.DisplayCharName = displayCharName;
				Cfg.Save(Plugin);
			}

			ImGui.EndTabItem();
		}

		// Overlay

		public void DrawOverlayTab() {
			var drawLines = Cfg.DrawLinesOnSkeleton;
			if (ImGui.Checkbox("Draw lines on skeleton", ref drawLines)) {
				Cfg.DrawLinesOnSkeleton = drawLines;
				Cfg.Save(Plugin);
			}

			var lineThickness = Cfg.SkeletonLineThickness;
			if (ImGui.SliderFloat("Lines thickness", ref lineThickness, 0.01F, 15F, "%.1f")) {
				Cfg.SkeletonLineThickness = lineThickness;
				Cfg.Save(Plugin);
			}

			ImGui.Separator();

			bool linkBoneCategoriesColors = Cfg.LinkBoneCategoryColors;
			if (ImGuiComponents.IconButton(FontAwesomeIcon.Link, linkBoneCategoriesColors ? new Vector4(0.0F, 1.0F, 0.0F, 0.4F) : null))
			{
				Cfg.LinkBoneCategoryColors = !linkBoneCategoriesColors;
				Cfg.Save(Plugin);
			}

			ImGui.SameLine();
			ImGui.Text(linkBoneCategoriesColors ? "Unlink bones colors" : "Link bones colors");

			ImGui.SameLine();
			if (ImGuiComponents.IconButton(FontAwesomeIcon.Eraser))
			{
				Vector4 eraseColor = new(1.0F, 1.0F, 1.0F, 0.5647059F);
				if(linkBoneCategoriesColors)
					Cfg.LinkedBoneCategoryColor = eraseColor;
				else
					foreach (Category category in Category.Categories.Values)
						if (category.ShouldDisplay || Cfg.BoneCategoryColors.ContainsKey(category.Name))
							Cfg.BoneCategoryColors[category.Name] = eraseColor;
				Cfg.Save(Plugin);
			}


			if (linkBoneCategoriesColors)
			{
				Vector4 linkedBoneColor = Cfg.LinkedBoneCategoryColor;
				if (ImGui.ColorEdit4("Bones color", ref linkedBoneColor, ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.AlphaBar))
				{
					Cfg.LinkedBoneCategoryColor = linkedBoneColor;
					Cfg.Save(Plugin);
				}

			} else {

				ImGui.SameLine();
				if (ImGuiComponents.IconButton(FontAwesomeIcon.Rainbow))
				{
					foreach ((string categoryName, Category category) in Category.Categories)
					{
						if (!category.ShouldDisplay && !Cfg.BoneCategoryColors.ContainsKey(category.Name)) continue;
						Cfg.BoneCategoryColors[category.Name] = category.DefaultColor;
					}
					Cfg.Save(Plugin);
				}

				ImGui.Text("Bone colors by category");

				bool hasShownAnyCategory = false;
				foreach (Category category in Category.Categories.Values)
				{
					if (!category.ShouldDisplay && !Cfg.BoneCategoryColors.ContainsKey(category.Name)) continue;

					if (!Cfg.BoneCategoryColors.TryGetValue(category.Name, out Vector4 categoryColor))
						categoryColor = Cfg.LinkedBoneCategoryColor;

					if (ImGui.ColorEdit4(category.Name, ref categoryColor, ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.AlphaBar))
					{
						Cfg.BoneCategoryColors[category.Name] = categoryColor;
						Cfg.Save(Plugin);
					}
					hasShownAnyCategory = true;
				}
				if (!hasShownAnyCategory) ImGui.TextWrapped("Categories will be added after bones are displayed once.");
			}

			ImGui.EndTabItem();
		}

		// Gizmo

		public void DrawGizmoTab() {
			var allowAxisFlip = Cfg.AllowAxisFlip;
			if (ImGui.Checkbox("Flip axis to face camera", ref allowAxisFlip)) {
				Cfg.AllowAxisFlip = allowAxisFlip;
				Cfg.Save(Plugin);
			}

			ImGui.EndTabItem();
		}

		// Language

		public void DrawLanguageTab() {
			var selected = "";
			foreach (var lang in Locale.Languages) {
				if (lang == Cfg.Localization) {
					selected = $"{lang}";
					break;
				}
			}

			if (ImGui.BeginCombo("Language", selected)) {
				foreach (var lang in Locale.Languages) {
					var name = $"{lang}";
					if (ImGui.Selectable(name, name == selected)) {
						Cfg.Localization = lang;
						Cfg.Save(Plugin);
					}
				}

				ImGui.SetItemDefaultFocus();
				ImGui.EndCombo();
			}

			var translateBones = Cfg.TranslateBones;
			if (ImGui.Checkbox("Translate bone names", ref translateBones)) {
				Cfg.TranslateBones = translateBones;
				Cfg.Save(Plugin);
			}

			ImGui.EndTabItem();
		}
	}
}
