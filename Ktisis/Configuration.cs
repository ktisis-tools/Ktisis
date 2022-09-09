using System;

using System.Collections.Generic;
using System.Numerics;
using Dalamud;
using Dalamud.Configuration;

using Ktisis.Helpers;
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
		public UInt32 CategoryColor(Bone bone)
		{
			Vector4 color = LinkedBoneCategoryColor;
			if (LinkBoneCategoryColors) color = LinkedBoneCategoryColor;
			else if (BoneCategoryColors.ContainsKey(bone.Category)) color = BoneCategoryColors[bone.Category];
			return MathHelpers.ConvertVector4ToUInt32(color);
		}
		public bool IsBoneVisible(Bone bone)
		{
			bool showBone = true;
			if (ShowBoneByCategory.ContainsKey(bone.Category)) showBone = ShowBoneByCategory[bone.Category];
			return DrawLinesOnSkeleton && showBone;
		}

		public bool IsBoneCategoryVisible(Category category)
		{
			if (ShowBoneByCategory.ContainsKey(category)) return ShowBoneByCategory[category];
			return true;
		}


		// Gizmo

		public bool AllowAxisFlip { get; set; } = true;

		// Language

		public UserLocale Localization { get; set; } = UserLocale.English;
		public ClientLanguage SheetLocale { get; set; } = ClientLanguage.English;

		public bool TranslateBones = true;

		// UI memory

		public bool ShowSkeleton { get; set; } = false;
		public Dictionary<Category, bool> ShowBoneByCategory = new();
		public bool LinkBoneCategoryColors { get; set; } = false;
		public Vector4 LinkedBoneCategoryColor { get; set; } = new Vector4(1.0F, 1.0F, 1.0F, 0.5647059F);
		public Dictionary<Category, Vector4> BoneCategoryColors = new();

		// save

		public void Save(Ktisis plugin) {
			plugin.PluginInterface.SavePluginConfig(this);
		}
	}
}