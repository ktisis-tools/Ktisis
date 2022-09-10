using System.Collections.Generic;
using System.Numerics;

namespace Ktisis.Structs.Bones
{
	public class Category
	{
		public string Name { get; set; }
		public Vector4 DefaultColor { get; set; }
		public readonly List<string> DetectedBones = new();
		public readonly List<string> PossibleBones = new();


		public static readonly Dictionary<string, Category> Categories = new();

		private Category(string name, Vector4 defaultColor)
		{
			Name = name;
			DefaultColor = defaultColor;
			foreach ((string boneName, string categoryName) in BoneCategories.BonesCategoriesAssociation)
				if (Name == categoryName) PossibleBones.Add(boneName);
		}

		public static Category CreateCategory(string name, Vector4 defaultColor)
		{
			/* TODO: We currently throw for duplicated categories. This may turn out to be a problem in the future. */
			Category cat = new(name, defaultColor);
			Categories.Add(name, cat);
			return cat;
		}

		public bool IsEmpty() => DetectedBones.Count == 0;

		public void RegisterBone(string boneName)
		{
			if(!DetectedBones.Contains(boneName))
				DetectedBones.Add(boneName);
		}

		internal void Deconstruct(out string name, out Vector4 defaultColor)
		{
			name = Name;
			defaultColor = DefaultColor;
		}
		internal void Deconstruct(out string name) => name = Name;

		static Category()
		{
			Vector4 defaultColor = new Vector4(1.0F, 1.0F, 1.0F, 0.5647059F);

			/* Default fallback category */
			CreateCategory("custom", new Vector4(1.0F, 1.0F, 1.0F, 0.5647059F));

			CreateCategory("body", new Vector4(1.0F, 0.0F, 0.0F, 0.5647059F));
			CreateCategory("head", new Vector4(0.0F, 1.0F, 0.0F, 0.5647059F));
			CreateCategory("hair", new Vector4(0.0F, 0.0F, 1.0F, 0.5647059F));
			CreateCategory("clothes", new Vector4(1.0F, 1.0F, 0.0F, 0.5647059F));
			CreateCategory("right hand", new Vector4(1.0F, 0.0F, 1.0F, 0.5647059F));
			CreateCategory("left hand", new Vector4(0.0F, 1.0F, 1.0F, 0.5647059F));
			CreateCategory("tail", defaultColor);
			CreateCategory("ears", defaultColor);
			CreateCategory("feet", defaultColor);
		}
	}
}
