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
using Ktisis.Structs.Actor.Equip.SetSources;
using static Ktisis.Data.Files.AnamCharaFile;

namespace Ktisis.Legacy.Config {
	[Serializable]
	public class ConfigV2 : IPluginConfiguration {
		public int Version { get; set; }

		public bool IsFirstTimeInstall = true;
		public string LastPluginVer = "";

		// Interface

		public bool AutoOpen = true;

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

		public Configuration Migrate() {
			if (Version < 1 && AutoOpenCtor)
				OpenKtisisMethod = OpenKtisisMethod.OnPluginLoad;

			if (Version < 2 && ((int)PoseMode & 4) != 0)
				PoseMode ^= (PoseMode)4;

			return new Configuration() {
				// Ktisis
				OpenKtisisMethod = OpenKtisisMethod,
				// Overlay
				ShowOverlay = ShowSkeleton,
				SkeletonDotRadius = SkeletonDotRadius,
				SkeletonLineOpacity = SkeletonLineOpacity,
				SkeletonLineThickness = SkeletonLineThickness,
				SkeletonLineOpacityWhileUsing = SkeletonLineOpacityWhileUsing,
				// Gizmo
				GizmoMode = GizmoMode,
				GizmoOp = GizmoOp,
				SiblingLink = SiblingLink,
				AllowAxisFlip = AllowAxisFlip,
				// Targeting
				AllowTargetOnLeftClick = !DisableChangeTargetOnLeftClick,
				AllowTargetOnRightClick = !DisableChangeTargetOnRightClick,
				// UI
				CensorNsfw = CensorNsfw,
				DisplayCharName = DisplayCharName,
				EnableParenting = EnableParenting,
				// Language
				TranslateBones = TranslateBones,
				Language = Localization,
				// Keybinds
				EnableKeybinds = EnableKeybinds,
				// Data
				CustomBoneOffset = CustomBoneOffset,
				GlamourPlateData = GlamourPlateData
			};
		}
	}
}
