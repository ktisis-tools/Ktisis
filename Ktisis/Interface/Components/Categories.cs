using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using Dalamud.Interface;
using ImGuiNET;

using Ktisis.Interface.Windows.Workspace;
using Ktisis.Localization;
using Ktisis.Structs.Bones;
using Ktisis.Util;

namespace Ktisis.Interface.Components {
	internal static class Categories {

		private static bool DrawList(Func<Category,bool> drawForEach) {
			ImGui.Columns(2);
			int i = 0;
			bool hasShownAnyCategory = false;
			foreach (Category category in Category.Categories.Values) {
				if (!category.ShouldDisplay)
					continue;
				if (Ktisis.Configuration.CensorNsfw && category.IsNsfw)
					continue;

				if (!drawForEach(category)) continue;

				if (i % 2 != 0) ImGui.NextColumn();
				i++;
				hasShownAnyCategory = true;
			}
			ImGui.Columns();
			return hasShownAnyCategory;
		}


		public static bool DrawConfigList(Configuration cfg) {
			return DrawList(category => {
				if (!cfg.BoneCategoryColors.TryGetValue(category.Name, out Vector4 categoryColor))
					categoryColor = cfg.LinkedBoneCategoryColor;

				if (ImGui.ColorEdit4(Locale.GetString(category.Name), ref categoryColor, ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.AlphaBar))
					cfg.BoneCategoryColors[category.Name] = categoryColor;

				return true;
			});
		}

		public static bool DrawToggleList(Configuration cfg) {
			if (!DrawList(category => {

				// display checkboxes
				bool isOverloaded = Category.VisibilityOverload.Any(c => c == category.Name);
				bool categoryState = isOverloaded || cfg.IsBoneCategoryVisible(category);
				if (isOverloaded) ImGui.PushStyleColor(ImGuiCol.CheckMark, Workspace.ColGreen);
				var changed = ImGui.Checkbox(Locale.GetString(category.Name), ref categoryState);
				if (isOverloaded) ImGui.PopStyleColor();

				// clicks and modifiers handles
				if (ImGui.GetIO().KeyShift) {
					if (ImGui.IsItemClicked(ImGuiMouseButton.Left))
						category.ToggleVisibilityOverload();
					else if (ImGui.IsItemClicked(ImGuiMouseButton.Right)) {
						Category.VisibilityOverload.Clear();
						category.ToggleVisibilityOverload();
					}

				} else if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
					SelectOnlyCategory(category);
				else if (changed)
					cfg.ShowBoneByCategory[category.Name] = categoryState;

				return true;
			})) return false;

			// buttons to deselect or select all categories
			if (Category.VisibilityOverload.Any()) {
				ImGui.PushStyleColor(ImGuiCol.Text, Workspace.ColGreen);
				if (GuiHelpers.IconButton(FontAwesomeIcon.Times, new(ControlButtons.ButtonSize.X * 2 + ImGui.GetStyle().ItemSpacing.X, ControlButtons.ButtonSize.Y)))
					Category.VisibilityOverload.Clear();
				ImGui.PopStyleColor();
				if (ImGui.IsItemHovered()) {
					ImGui.BeginTooltip();
					ImGui.Text("Clear category");
					ImGui.SameLine();
					ImGui.TextColored(Workspace.ColGreen, "overload visibility");
					ImGui.EndTooltip();
				}
			} else {
				if (GuiHelpers.IconButtonTooltip(FontAwesomeIcon.CheckDouble, "Select all categories", ControlButtons.ButtonSize))
					cfg.ShowBoneByCategory = cfg.ShowBoneByCategory.ToDictionary(p => p.Key, p => true);
				ImGui.SameLine();
				if (GuiHelpers.IconButtonTooltip(FontAwesomeIcon.Times, "Deselect all categories", ControlButtons.ButtonSize))
					foreach (var key in Category.Categories.Keys)
						cfg.ShowBoneByCategory[key] = false;
			}
			ImGui.SameLine(ImGui.GetContentRegionAvail().X - (ImGui.GetStyle().ItemSpacing.X) - GuiHelpers.CalcIconSize(FontAwesomeIcon.InfoCircle).X);
			ControlButtons.VerticalAlignTextOnButtonSize();

			// help hover
			GuiHelpers.Icon(FontAwesomeIcon.InfoCircle, false);
			if (ImGui.IsItemHovered()) {
				ImGui.BeginTooltip();
				ImGui.Text("Tips:");
				ImGui.BulletText("Left click to toggle a category");
				ImGui.BulletText("Right click to select the desired category only");
				ImGui.BulletText("Shift + Left click to toggle the category into");
				ImGui.SameLine(); ImGui.TextColored(Workspace.ColGreen, "overload visibility");
				ImGui.BulletText("Shift + Right click to select the desired");
				ImGui.SameLine(); ImGui.TextColored(Workspace.ColGreen, "overload visibility"); ImGui.SameLine();
				ImGui.Text("only");
				ImGui.BulletText("Keyboard shortcuts can be bound to individual category");
				ImGui.SameLine(); ImGui.TextColored(Workspace.ColGreen, "overload visibility"); ImGui.SameLine();
				ImGui.Text("hold or toggle");
				ImGui.EndTooltip();
			}

			return true;
		}
		public static void SelectOnlyCategory(Category category) {
			Ktisis.Configuration.ShowBoneByCategory = Ktisis.Configuration.ShowBoneByCategory.ToDictionary(p => p.Key, p => false);
			Ktisis.Configuration.ShowBoneByCategory[category.Name] = true;
		}
	}
}
