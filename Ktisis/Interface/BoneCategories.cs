using System;
using System.IO;
using System.Collections.Generic;

using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

using Ktisis.Interface.Library;

namespace Ktisis.Interface {
	public class BoneCategory {
		public string Name = "";

		public bool IsNsfw = false;

		public List<string> Bones = new();
		public List<BoneCategory> SubCategories = new();

		public BoneCategory? ParentCategory = null;
	}

	public static class BoneCategories {
		public static Dictionary<string, BoneCategory> Categories = new();

		private static Dictionary<string, BoneCategory> BoneCategoryIndex = new();

		private static BoneCategory OTHER_CATEGORY = new() { Name = "Other" };

		public static BoneCategory GetBoneCategory(string bone) {
			if (bone.StartsWith("j_ex_h")) {
				var isHair = bone.Length > 6 && bone[6] >= 48 && bone[6] <= 57; // 5th char is numeric
				if (isHair && Categories.TryGetValue("Hair", out var hair))
					return hair;
			}

			if (BoneCategoryIndex.TryGetValue(bone, out var cat))
				return cat;

			return OTHER_CATEGORY;
		}

		static BoneCategories() {
			try {
				var stream = Common.GetAssemblyFile("Data.Schema.BoneCategories.json");
				var file = new StreamReader(stream);
				using (var reader = new JsonTextReader(file)) {
					var jObject = (JArray)JToken.ReadFrom(reader);
					var categories = jObject.ToObject<List<BoneCategory>>()!;

					Logger.Verbose($"Loading {categories.Count} categories");

					foreach (var cat in categories)
						Add(cat);
				}
			} catch (Exception e) {
				Logger.Error(e, "Failed to fetch bone category schema");
			}

			Add(OTHER_CATEGORY);
		}

		private static void Add(BoneCategory category) {
			Categories.Add(category.Name, category);

			foreach (var bone in category.Bones) {
				BoneCategoryIndex.Add(bone, category);
			}

			foreach (var child in category.SubCategories) {
				child.ParentCategory = category;
				Add(child);
			}
		}
	}
}