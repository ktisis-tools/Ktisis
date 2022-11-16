using ImGuiNET;
using Ktisis.Localization;
using Ktisis.Structs.Bones;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

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
			return DrawList(category => {
				bool categoryState = cfg.IsBoneCategoryVisible(category);
				if (ImGui.Checkbox(Locale.GetString(category.Name), ref categoryState))
					cfg.ShowBoneByCategory[category.Name] = categoryState;

				return true;
			});
		}

	}
}
