using System;
using System.Linq;
using System.Numerics;
using System.Collections.Generic;

using ImGuizmoNET;

using Dalamud;
using Dalamud.Configuration;
using Dalamud.Game.ClientState.Keys;

using Ktisis.Posing;
using Ktisis.Services;
using Ktisis.Interface;
using Ktisis.Structs.Bones;
using Ktisis.Structs.Actor.Equip.SetSources;
using static Ktisis.Data.Files.AnamCharaFile;

namespace Ktisis {
	[Serializable]
	public class Configuration : IPluginConfiguration {
		public const int CurVersion = 2;
		public int Version { get; set; } = CurVersion;

		public bool IsFirstTimeInstall = true;
		public string LastPluginVer = "";

		// Interface
		[Obsolete("Replaced by AutoOpenCtor")]
		public bool AutoOpen = true;
		[Obsolete("Replaced by OpenKtisisMethod")]
		public bool AutoOpenCtor = false;
		public OpenKtisisMethod OpenKtisisMethod = OpenKtisisMethod.OnEnterGpose;

		public bool DisplayCharName = true;
		public bool CensorNsfw = false;

		public bool TransformTableDisplayMultiplierInputs = false;
		public float TransformTableBaseSpeedPos = 0.0005f;
		public float TransformTableBaseSpeedRot = 0.1f;
		public float TransformTableBaseSpeedSca = 0.001f;
		public float TransformTableModifierMultCtrl = 0.1f;
		public float TransformTableModifierMultShift = 10f;
		public int TransformTableDigitPrecision = 3;
		public float CustomWidthMarginDebug = 0.1f;

		// Input
		public bool EnableKeybinds = false;
		public Dictionary<Input.Purpose, List<VirtualKey>> KeyBinds = new();

		public bool DisableChangeTargetOnLeftClick = false;
		public bool DisableChangeTargetOnRightClick = false;

		// Overlay

		public bool DrawLinesOnSkeleton = true;
		public bool DrawLinesWithGizmo = true;
		public bool DrawDotsWithGizmo = true;

		public float SkeletonLineThickness = 2.0F;
		public float SkeletonLineOpacity = 0.95F;
		public float SkeletonLineOpacityWhileUsing = 0.15F;
		public float SkeletonDotRadius = 3.0F;

		// References
		// The reference Key creates a uniqueness constraint for imgui window IDs for each reference.
		public Dictionary<int, ReferenceInfo> References = new();
		public float ReferenceAlpha = 1.0f;
		public bool ReferenceHideDecoration = false;
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

		public MODE GizmoMode = MODE.LOCAL;
		public OPERATION GizmoOp = OPERATION.ROTATE;

		public SiblingLink SiblingLink = SiblingLink.None;

		public bool AllowAxisFlip = true;

		// Language

		public UserLocale Localization = UserLocale.English;
		public ClientLanguage SheetLocale = ClientLanguage.English;

		public bool TranslateBones = true;

		// UI memory

		public bool ShowSkeleton = false;
		public Dictionary<string, bool> ShowBoneByCategory = new();
		public bool LinkBoneCategoryColors = false;
		public Vector4 LinkedBoneCategoryColor = new(1.0F, 1.0F, 1.0F, 0.5647059F);
		public Dictionary<string, Vector4> BoneCategoryColors = new();

		public SaveModes CharaMode = SaveModes.All;
		public PoseMode PoseMode = PoseMode.All;
		public PoseTransforms PoseTransforms = PoseTransforms.Rotation;

		public bool EnableParenting = true;

		public bool LinkedGaze = true;
		
		public bool ShowToolbar = false;

		public Dictionary<string, string> SavedDirPaths = new();

		// Data memory
		public Dictionary<string, GlamourDresser.GlamourPlate[]?>? GlamourPlateData = null;
		public Dictionary<string, Dictionary<string, Vector3>> CustomBoneOffset = new();

		// Validate for changes in config versions.

		public void Validate() {
			if (Version == CurVersion)
				return;

			Logger.Warning($"Updating config to reflect changes between config versions {Version}-{CurVersion}.\nThis is nothing to worry about, but some settings may change or get reset!");

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
