using System;
using System.Linq;

namespace Ktisis.Structs.Bones {
	public class BoneCategories {
		public static Category DefaultCategory => Category.Categories["custom"];


		public static Category GetCategory(string? boneName)
		{
			if (boneName == null || boneName == "")
				return DefaultCategory;

			Category? category = null;
			foreach ((string categoryName, Category posibleCategory) in Category.Categories)
			{
				if (posibleCategory.PossibleBones.Contains(boneName ?? ""))
					category = posibleCategory;
			}

			category ??= DefaultCategory;

			category.RegisterBone(boneName);

			return category;
		}
		public static string GetCategoryName(string? boneName) => GetCategory(boneName).Name;

	}
}
