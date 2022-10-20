using System;
using System.Numerics;
using System.Collections.Generic;

using Dalamud;
using Dalamud.Configuration;

using Ktisis.Localization;
using Ktisis.Structs.Bones;

namespace Ktisis {
	[Serializable]
	public class Configuration : IPluginConfiguration {
		public int Version { get; set; } = 0;

		// Interface

		public bool AutoOpen { get; set; } = true;

		public bool DisplayCharName = true;

		// Overlay

		public bool DrawLinesOnSkeleton { get; set; } = true;
		public float SkeletonLineThickness { get; set; } = 2.0F;

		/*public Vector4 GetCategoryColor(Bone bone)
		{
			if (LinkBoneCategoryColors) return LinkedBoneCategoryColor;
			if (!BoneCategoryColors.TryGetValue(bone.Category.Name, out Vector4 color))
				return LinkedBoneCategoryColor;

			return color;
		}

		public bool IsBoneVisible(Bone bone)
		{
			if (!ShowBoneByCategory.TryGetValue(bone.Category.Name, out bool boneVisible))
				return DrawLinesOnSkeleton;
			return DrawLinesOnSkeleton && boneVisible;
		}*/

		public bool IsBoneCategoryVisible(Category category)
		{
			if (!ShowBoneByCategory.TryGetValue(category.Name, out bool boneCategoryVisible))
				return true;
			return boneCategoryVisible;
		}

		// Gizmo

		public bool AllowAxisFlip { get; set; } = true;

		// Language

		public UserLocale Localization { get; set; } = UserLocale.English;
		public ClientLanguage SheetLocale { get; set; } = ClientLanguage.English;

		public bool TranslateBones = true;

		// UI memory

		public bool ShowSkeleton { get; set; } = false;
		public Dictionary<string, bool> ShowBoneByCategory = new();
		public bool LinkBoneCategoryColors { get; set; } = false;
		public Vector4 LinkedBoneCategoryColor { get; set; } = new(1.0F, 1.0F, 1.0F, 0.5647059F);
		public Dictionary<string, Vector4> BoneCategoryColors = new();

		public bool LinkedGaze { get; set; } = true;
	}
}
