using System;
using System.Linq;
using System.Numerics;
using System.Collections.Generic;

using ImGuizmoNET;

using Dalamud;
using Dalamud.Logging;
using Dalamud.Configuration;
using Dalamud.Game.ClientState.Keys;

using Ktisis.Interface;
using Ktisis.Localization;
using Ktisis.Structs.Bones;
using Ktisis.Structs.Actor.Equip.SetSources;
using Ktisis.Structs.Poses;
using static Ktisis.Data.Files.AnamCharaFile;

namespace Ktisis
{
    [Serializable]
	public class Configuration : IPluginConfiguration {
		public const int CurVersion = 2;
		public int Version { get; set; } = CurVersion;

		public bool IsFirstTimeInstall { get; set; } = true;
		public string LastPluginVer { get; set; } = "";

		// Interface
		[Obsolete("Replaced by AutoOpenCtor")]
		public bool AutoOpen { get; set; } = true;
		[Obsolete("Replaced by OpenKtisisMethod")]
		public bool AutoOpenCtor { get; set; } = false;
		public OpenKtisisMethod OpenKtisisMethod { get; set; } = OpenKtisisMethod.OnEnterGpose;

		public bool DisplayCharName { get; set; } = true;
		public bool CensorNsfw { get; set; } = true;

		public bool TransformTableDisplayMultiplierInputs { get; set; } = false;
		public float TransformTableBaseSpeedPos { get; set; } = 0.0005f;
		public float TransformTableBaseSpeedRot { get; set; } = 0.1f;
		public float TransformTableBaseSpeedSca { get; set; } = 0.001f;
		public float TransformTableModifierMultCtrl { get; set; } = 0.1f;
		public float TransformTableModifierMultShift { get; set; } = 10f;
		public int TransformTableDigitPrecision { get; set; } = 3;

		// Input
		public bool EnableKeybinds { get; set; } = true;
		public Dictionary<Input.Purpose, List<VirtualKey>> KeyBinds { get; set; } = new();

		public bool DisableChangeTargetOnLeftClick { get; set; } = false;
		public bool DisableChangeTargetOnRightClick { get; set; } = false;

		// Overlay

		public bool DrawLinesOnSkeleton { get; set; } = true;
		public bool DrawLinesWithGizmo { get; set; } = true;
		public bool DrawDotsWithGizmo { get; set; } = true;

		public float SkeletonLineThickness { get; set; } = 2.0F;
		public float SkeletonDotRadius { get; set; } = 3.0F;

		// References
		// The reference Key creates a uniqueness constraint for imgui window IDs for each reference.
		public Dictionary<int, ReferenceInfo> References { get; set; } = new();
		public float ReferenceAlpha { get; set; } = 1.0f;
		public bool ReferenceHideDecoration { get; set; } = false;
		public int NextReferenceKey => References.Count == 0 ? 0 : References.Max(x => x.Key) + 1;

		public Vector4 GetCategoryColor(Bone bone) {
			if (LinkBoneCategoryColors) return LinkedBoneCategoryColor;
			// pick the first category found
			foreach (var category in bone.Categories)
				if (IsBoneCategoryVisible(category) && BoneCategoryColors.TryGetValue(category.Name, out Vector4 color))
					return color;
			return LinkedBoneCategoryColor;
		}

		public bool IsBoneVisible(Bone bone) {
			// Check if input is forcing a category to show solo
			if (Category.VisibilityOverload.Count > 0)
				if (Category.VisibilityOverload.Intersect(bone.Categories.Select(c => c.Name)).Any())
					return true;
				else
					return false;

			if (CensorNsfw && bone.Categories.Any(c => c.IsNsfw))
				return false;

			// bone will be visible if any category is visible
			foreach (var category in bone.Categories)
				if (ShowBoneByCategory.GetValueOrDefault(category.Name, true))
					return true;

			return false;
		}

		public bool IsBoneCategoryVisible(Category category) {
			if (!ShowBoneByCategory.TryGetValue(category.Name, out bool boneCategoryVisible))
				return true;
			return boneCategoryVisible;
		}

		// Gizmo

		public MODE GizmoMode { get; set; } = MODE.LOCAL;
		public OPERATION GizmoOp { get; set; } = OPERATION.ROTATE;

		public SiblingLink SiblingLink { get; set; } = SiblingLink.None;

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

		public SaveModes CharaMode { get; set; } = SaveModes.All;
		public PoseMode PoseMode { get; set; } = PoseMode.All;
		public PoseTransforms PoseTransforms { get; set; } = PoseTransforms.Rotation;

		public bool EnableParenting { get; set; } = true;

		public bool LinkedGaze { get; set; } = true;
		
		public bool ShowToolbar { get; set; } = false;

		public Dictionary<string, string> SavedDirPaths { get; set; } = new();

		// Data memory
		public Dictionary<string, GlamourDresser.GlamourPlate[]?>? GlamourPlateData { get; set; } = null;
		public Dictionary<string, Dictionary<string, Vector3>> CustomBoneOffset { get; set; } = new();

		// Validate for changes in config versions.

		public void Validate() {
			if (Version == CurVersion)
				return;

			PluginLog.Warning($"Updating config to reflect changes between config versions {Version}-{CurVersion}.\nThis is nothing to worry about, but some settings may change or get reset!");

#pragma warning disable CS0612, CS0618
			// Apply changes from obsolete attributes

			if (Version < 1)
				if (AutoOpenCtor) OpenKtisisMethod = OpenKtisisMethod.OnPluginLoad;

#pragma warning restore CS0612, CS0618

			if (Version < 2) {
				if (((int)PoseMode & 4) != 0)
					PoseMode ^= (PoseMode)4;
			}

			Version = CurVersion;
		}
	}
	[Serializable]
	public class ReferenceInfo {
		public bool Showing { get; set; }
		public string? Path { get; set; }
	}
	[Serializable]
	public enum OpenKtisisMethod {
		Manually,
		OnPluginLoad,
		OnEnterGpose,
	}
}