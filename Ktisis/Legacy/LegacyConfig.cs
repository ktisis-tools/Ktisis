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

		public bool? TransformTableDisplayMultiplierInputs { get; set; } = false;
		public float? TransformTableBaseSpeedPos { get; set; } = 0.0005f;
		public float? TransformTableBaseSpeedRot { get; set; } = 0.1f;
		public float? TransformTableBaseSpeedSca { get; set; } = 0.001f;
		public float? TransformTableModifierMultCtrl { get; set; } = 0.1f;
		public float? TransformTableModifierMultShift { get; set; } = 10f;
		public int? TransformTableDigitPrecision { get; set; } = 3;
		public float? CustomWidthMarginDebug { get; set; } = 0.1f;

		// Input
		public bool? EnableKeybinds { get; set; } = false;

		public Dictionary<Input.Purpose, List<VirtualKey>> KeyBinds { get; set; } = new();

		public bool? DisableChangeTargetOnLeftClick { get; set; } = false;
		public bool? DisableChangeTargetOnRightClick { get; set; } = false;

		// Overlay

		public bool? OrderBoneListByDistance { get; set; } = false;

		public bool? DrawLinesOnSkeleton { get; set; } = true;
		public bool? DrawLinesWithGizmo { get; set; } = true;
		public bool? DrawDotsWithGizmo { get; set; } = true;

		public float? SkeletonLineThickness { get; set; } = 2.0F;
		public float? SkeletonLineOpacity { get; set; } = 0.95F;
		public float? SkeletonLineOpacityWhileUsing { get; set; } = 0.15F;
		public float? SkeletonDotRadius { get; set; } = 3.0F;

		//AutoSave
		public bool? EnableAutoSave { get; set; } = false;
		public int? AutoSaveInterval { get; set; } = 60;
		public int? AutoSaveCount { get; set; } = 5;
		public string? AutoSavePath { get; set; } 
		public string? AutoSaveFormat { get; set; } = "AutoSave - %Date% %Time%";
		public bool? ClearAutoSavesOnExit { get; set; } = false;
		
		
		public bool? AllowAxisFlip { get; set; } = true;
		
		
		public Dictionary<string, bool>? ShowBoneByCategory = new();
		public bool? LinkBoneCategoryColors { get; set; } = false;
		public Vector4? LinkedBoneCategoryColor { get; set; } = new(1.0F, 1.0F, 1.0F, 0.5647059F);
		public Dictionary<string, Vector4>? BoneCategoryColors = new();
		
		
		public bool? PositionWeapons { get; set; } = true;

		public bool? EnableParenting { get; set; } = true;

		public bool? LinkedGaze { get; set; } = true;

		public bool? ShowToolbar { get; set; } = false;

		public Dictionary<string, string>? SavedDirPaths { get; set; } = new();

		// Camera

		public float? FreecamMoveSpeed { get; set; } = 0.1f;

		public float? FreecamShiftMuli { get; set; } = 2.5f;
		public float? FreecamCtrlMuli { get; set; } = 0.25f;
		public float? FreecamUpDownMuli { get; set; } = 1f;

		public float? FreecamSensitivity { get; set; } = 0.215f;

		public Keybind? FreecamForward { get; set; }
		public Keybind? FreecamLeft { get; set; } 
		public Keybind? FreecamBack { get; set; } 
		public Keybind? FreecamRight { get; set; }
		public Keybind? FreecamUp { get; set; }
		public Keybind? FreecamDown { get; set; }

		public Keybind? FreecamFast { get; set; }
		public Keybind? FreecamSlow { get; set; } 

		// Data memory
		public Dictionary<string, Dictionary<string, Vector3>>? CustomBoneOffset { get; set; } = new();
		
		
	}

	[Serializable]
	public class Keybind { }

	
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