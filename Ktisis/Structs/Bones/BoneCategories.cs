using System;
using System.Linq;

namespace Ktisis.Structs.Bones {
	public class BoneCategories {
		public static string GetCategoryName(string? boneName) => Category.GetForBone(boneName).Name;

	}
}
