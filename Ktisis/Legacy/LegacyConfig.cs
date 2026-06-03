using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text.Json.Serialization;

using Dalamud.Configuration;
using Dalamud.Game.ClientState.Keys;

namespace Ktisis.Legacy;

public class LegacyConfig {
	[Serializable]
	public class Configuration: IPluginConfiguration {

		public const int CurVersion = 3;
		public int Version { get; set; } = CurVersion;

		public bool? DisplayCharName { get; set; }
		public bool? CensorNsfw { get; set; }

		public bool? TransformTableDisplayMultiplierInputs { get; set; }
		public float? TransformTableBaseSpeedPos { get; set; }
		public float? TransformTableBaseSpeedRot { get; set; }
		public float? TransformTableBaseSpeedSca { get; set; }
		public float? TransformTableModifierMultCtrl { get; set; }
		public float? TransformTableModifierMultShift { get; set; }
		public int? TransformTableDigitPrecision { get; set; }

		// Input
		public bool? EnableKeybinds { get; set; }

		public Dictionary<Input.Purpose, List<VirtualKey>> KeyBinds { get; set; }

		public bool? DisableChangeTargetOnLeftClick { get; set; }
		public bool? DisableChangeTargetOnRightClick { get; set; }

		// Overlay

		public bool? OrderBoneListByDistance { get; set; }

		public bool? DrawLinesOnSkeleton { get; set; }
		public bool? DrawLinesWithGizmo { get; set; }
		public bool? DrawDotsWithGizmo { get; set; }

		public float? SkeletonLineThickness { get; set; }
		public float? SkeletonLineOpacity { get; set; }
		public float? SkeletonLineOpacityWhileUsing { get; set; }
		public float? SkeletonDotRadius { get; set; }

		//AutoSave
		public bool? EnableAutoSave { get; set; }
		public int? AutoSaveInterval { get; set; }
		public int? AutoSaveCount { get; set; }
		public string? AutoSavePath { get; set; } 
		public string? AutoSaveFormat { get; set; }
		public bool? ClearAutoSavesOnExit { get; set; }
		
		
		public bool? AllowAxisFlip { get; set; }


		public Dictionary<string, bool>? ShowBoneByCategory;
		public bool? LinkBoneCategoryColors { get; set; }
		public Vector4? LinkedBoneCategoryColor { get; set; }
		public Dictionary<string, Vector4>? BoneCategoryColors;
		
		
		public bool? PositionWeapons { get; set; }

		public bool? EnableParenting { get; set; }

		public bool? LinkedGaze { get; set; }
		

		public Dictionary<string, string>? SavedDirPaths { get; set; }

		// Camera

		public float? FreecamMoveSpeed { get; set; } 

		public float? FreecamShiftMuli { get; set; }
		public float? FreecamCtrlMuli { get; set; }
		public float? FreecamUpDownMuli { get; set; }

		public float? FreecamSensitivity { get; set; } 

		public Keybind? FreecamForward { get; set; }
		public Keybind? FreecamLeft { get; set; } 
		public Keybind? FreecamBack { get; set; } 
		public Keybind? FreecamRight { get; set; }
		public Keybind? FreecamUp { get; set; }
		public Keybind? FreecamDown { get; set; }

		public Keybind? FreecamFast { get; set; }
		public Keybind? FreecamSlow { get; set; } 

		// Data memory
		public Dictionary<string, Dictionary<string, Vector3>>? CustomBoneOffset { get; set; } 
		
		
	}

	[Serializable]
	public class Keybind {
		public VirtualKey[] Keys = {};

		public Keybind(params VirtualKey[] keys) => Keys = keys;

	}

	
	[Serializable]
	public enum OpenKtisisMethod {
		Manually,
		OnPluginLoad,
		OnEnterGpose,
	}

	public class Input {
		[Serializable]
		public enum Purpose {
			GlobalModifierKey,
			SwitchToTranslate,
			SwitchToRotate,
			SwitchToScale,
			ToggleLocalWorld,
			HoldToHideSkeleton,
			SwitchToUniversal,
			ClearCategoryVisibilityOverload,
			HoldAllCategoryVisibilityOverload,
			CircleThroughSiblingLinkModes,
			DeselectGizmo,
			BoneSelectionUp,
			BoneSelectionDown,
			NextCamera,
			PreviousCamera,
			ToggleFreeCam,
			NewCamera,
		}
	}

}