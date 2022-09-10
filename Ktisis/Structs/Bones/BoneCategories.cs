using System;
using System.Linq;

namespace Ktisis.Structs.Bones {
	public class BoneCategories {
		public static Category DefaultCategory => Category.Categories["custom"];


		public static Category GetCategory(string? boneName)
		{
			if (string.IsNullOrEmpty(boneName))
				return DefaultCategory;

			Category? category;
			if(!Category.CategoriesByBone.TryGetValue(boneName, out category)) {
				category = DefaultCategory;
			}

			category.MarkForDisplay();

			return category;
		}
		public static string GetCategoryName(string? boneName) => GetCategory(boneName).Name;

	}
}
