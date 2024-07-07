using System;
using System.Linq;
using System.Numerics;
using System.Collections.Generic;
using System.IO;

using ImGuizmoNET;

using Dalamud;
using Dalamud.Configuration;
using Dalamud.Game;
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
		public const int CurVersion = 3;
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
		public bool CensorNsfw { get; set; } = false;

		public bool TransformTableDisplayMultiplierInputs { get; set; } = false;
		public float TransformTableBaseSpeedPos { get; set; } = 0.0005f;
		public float TransformTableBaseSpeedRot { get; set; } = 0.1f;
		public float TransformTableBaseSpeedSca { get; set; } = 0.001f;
		public float TransformTableModifierMultCtrl { get; set; } = 0.1f;
		public float TransformTableModifierMultShift { get; set; } = 10f;
		public int TransformTableDigitPrecision { get; set; } = 3;
		public float CustomWidthMarginDebug { get; set; } = 0.1f;

		// Input
		public bool EnableKeybinds { get; set; } = false;
		public Dictionary<Input.Purpose, List<VirtualKey>> KeyBinds { get; set; } = new();

		public bool DisableChangeTargetOnLeftClick { get; set; } = false;
		public bool DisableChangeTargetOnRightClick { get; set; } = false;

		// Overlay

		public bool OrderBoneListByDistance { get; set; } = false;

		public bool DrawLinesOnSkeleton { get; set; } = true;
		public bool DrawLinesWithGizmo { get; set; } = true;
		public bool DrawDotsWithGizmo { get; set; } = true;

		public float SkeletonLineThickness { get; set; } = 2.0F;
		public float SkeletonLineOpacity { get; set; } = 0.95F;
		public float SkeletonLineOpacityWhileUsing { get; set; } = 0.15F;
		public float SkeletonDotRadius { get; set; } = 3.0F;

		//AutoSave
		public bool EnableAutoSave { get; set; } = false;
		public int AutoSaveInterval { get; set; } = 60;
		public int AutoSaveCount { get; set; } = 5;
		public string AutoSavePath { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Ktisis", "PoseAutoBackup");
		public string AutoSaveFormat { get; set; } = "AutoSave - %Date% %Time%";
		public bool ClearAutoSavesOnExit { get; set; } = false;

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
		public PoseMode PoseMode { get; set; } = PoseMode.BodyFace;
		public PoseTransforms PoseTransforms { get; set; } = PoseTransforms.Rotation;
		public bool PositionWeapons { get; set; } = true;

		public bool EnableParenting { get; set; } = true;

		public bool LinkedGaze { get; set; } = true;

		public bool ShowToolbar { get; set; } = false;

		public Dictionary<string, string> SavedDirPaths { get; set; } = new();

		// Camera

		public float FreecamMoveSpeed { get; set; } = 0.1f;

		public float FreecamShiftMuli { get; set; } = 2.5f;
		public float FreecamCtrlMuli { get; set; } = 0.25f;
		public float FreecamUpDownMuli { get; set; } = 1f;

		public float FreecamSensitivity { get; set; } = 0.215f;

		public Keybind FreecamForward { get; set; } = new(VirtualKey.W);
		public Keybind FreecamLeft { get; set; } = new(VirtualKey.A);
		public Keybind FreecamBack { get; set; } = new(VirtualKey.S);
		public Keybind FreecamRight { get; set; } = new(VirtualKey.D);
		public Keybind FreecamUp { get; set; } = new(VirtualKey.SPACE);
		public Keybind FreecamDown { get; set; } = new(VirtualKey.Q);

		public Keybind FreecamFast { get; set; } = new(VirtualKey.SHIFT);
		public Keybind FreecamSlow { get; set; } = new(VirtualKey.CONTROL);

		// Data memory
		public Dictionary<string, GlamourDresser.GlamourPlate[]?>? GlamourPlateData { get; set; } = null;
		public Dictionary<string, Dictionary<string, Vector3>> CustomBoneOffset { get; set; } = new();

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

			if (Version < 3) {
				PoseMode ^= PoseMode.Weapons;
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
