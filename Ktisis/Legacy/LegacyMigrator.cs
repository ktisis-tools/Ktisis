using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;

using Dalamud.Bindings.ImGui;
using Dalamud.Configuration;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Plugin;

using FFXIVClientStructs;

using Ktisis.Actions;
using Ktisis.Actions.Binds;
using Ktisis.Common.Utility;
using Ktisis.Core.Attributes;
using Ktisis.Data.Config;
using Ktisis.Data.Config.Actions;
using Ktisis.Interface;
using Ktisis.Legacy.Interface;
using Ktisis.Localization;
using Ktisis.Services.Game;

namespace Ktisis.Legacy;

[Singleton]
public class LegacyMigrator {
	private readonly GPoseService _gpose;
	private readonly GuiManager _gui;
	private readonly IDalamudPluginInterface _dpi;
	private readonly ConfigManager _cfg;
	internal LegacyConfig.Configuration? _legacyCfg;
	internal Configuration _tempConfig;

	private readonly Dictionary<string, string> LegacyRaceSexMap = new() {
		{ "Midlander_Masculine", "101" },
		{ "Midlander_Feminine", "201" },
		{ "Highlander_Masculine", "301" },
		{ "Highlander_Feminine", "401" },
		{ "Elezen_Masculine", "501" },
		{ "Elezen_Feminine", "601" },
		{ "Miqote_Masculine", "701" },
		{ "Miqote_Feminine", "801" },
		{ "Roegadyn_Masculine", "901" },
		{ "Roegadyn_Feminine", "1001" },
		{ "Lalafell_Masculine", "1101" },
		{ "Lalafell_Feminine", "1201" },
		{ "AuRa_Masculine", "1301" },
		{ "AuRa_Feminine", "1401" },
		{ "Hrothgar_Masculine", "1501" },
		{ "Hrothgar_Feminine", "1601" },
		{ "Viera_Masculine", "1701" },
		{ "Viera_Feminine", "1801" },
	};

	private readonly Dictionary<string, string> LegacyCategoryMap = new Dictionary<string, string>() {
		{"clothes", "Clothing"},
		{"body", "Body"},
		{"eyes", "Eyes"},
		{"mouth", "Mouth"},
		{"face", "Face"},
		{"hair", "Hair"},
		{"weapons", "Weapons"},
		{"right hand", "RightHand"},
		{"left hand", "LeftHand"},
		{"tail", "Tail"},
		{"ears", "Ears"},
		{"ivcs left hand", "LeftHandIvcs"},
		{"ivcs right hand", "RightHandIvcs"},
		{"ivcs left foot", "LeftFootIvcs"},
		{"ivcs right foot", "RightFootIvcs"},
		{"ivcs penis", "PenisIvcs"},
		{"ivcs vagina", "VaginaIvcs"},
		{"ivcs buttocks", "BottomIvcs"}
	};
	public bool v2ConfigExists;
	public bool v3ConfigExists;

	public event Action? OnConfirmed;

	public LegacyMigrator(
		GPoseService gpose,
		GuiManager gui,
		IDalamudPluginInterface dpi,
		ConfigManager cfg
	) {
		this._gpose = gpose;
		this._gui = gui;
		this._dpi = dpi;
		this._cfg = cfg;
	}

	// Setup

	public void Setup() {
		this.v2ConfigExists = this._dpi.ConfigFile.Exists;
		this.v3ConfigExists = File.Exists(this._dpi.ConfigDirectory + "\\KtisisV3.json");
		this._tempConfig = this._cfg.GenerateOrLoad();
		
		Ktisis.Locale.Initialize(this._cfg);
		if (this.v2ConfigExists) {
			Ktisis.Log.Warning("User is migrating from Ktisis v0.2, activating legacy mode.");
			var configurations = new PluginConfigurations(new DirectoryInfo(this._dpi.GetPluginConfigDirectory()).Parent!.ToString());
			this._legacyCfg = configurations.LoadForType<LegacyConfig.Configuration>("Ktisis");
		} else
			Ktisis.Log.Warning("User is migrating from Ktisis v0.3 beta, activating legacy mode.");

		this._gpose.StateChanged += this.OnGPoseStateChanged;
		this._gpose.Subscribe();
	}

	private void OnGPoseStateChanged(object sender, bool state) {
		if (!state || this._confirmed) return;
		var window = this._gui.GetOrCreate<MigratorWindow>(this);
		window.Open();
	}

	internal void MigrateConfig() {

		var cfg = this._tempConfig = this._cfg.CreateDefault();
		
		// The big 3 so to speak
		cfg.Editor.IncognitoPlayerNames = this._legacyCfg?.DisplayCharName ?? cfg.Editor.IncognitoPlayerNames;
		cfg.Categories.ShowNsfwBones = !this._legacyCfg?.CensorNsfw ?? cfg.Categories.ShowNsfwBones;
		cfg.Keybinds.Enabled = this._legacyCfg?.EnableKeybinds ?? cfg.Keybinds.Enabled;

		cfg.Editor.UseToolbar = true; // teehee
		cfg.Editor.SelectOnTarget = true;
		cfg.Overlay.DimOverlayForInactiveActors = true; // set these also for v2 enjoyers

		// File
		if (this._legacyCfg?.SavedDirPaths?.Count > 0) {
			foreach (var path in this._legacyCfg?.SavedDirPaths!) {
				cfg.File.CustomLocations.Add((path.Key, path.Value));
			}
		}

		// Input
		cfg.Keybinds.BlockTargetLeftClick = this._legacyCfg?.DisableChangeTargetOnLeftClick ?? cfg.Keybinds.BlockTargetLeftClick;
		cfg.Keybinds.BlockTargetRightClick = this._legacyCfg?.DisableChangeTargetOnRightClick ?? cfg.Keybinds.BlockTargetRightClick;

		// Overlay
		cfg.Overlay.DrawLines = this._legacyCfg?.DrawLinesOnSkeleton ?? cfg.Overlay.DrawLines;
		cfg.Overlay.DrawLinesGizmo = this._legacyCfg?.DrawLinesWithGizmo ?? cfg.Overlay.DrawLinesGizmo;
		cfg.Overlay.DrawDotsGizmo = this._legacyCfg?.DrawDotsWithGizmo ?? cfg.Overlay.DrawDotsGizmo;
		cfg.Overlay.LineThickness = this._legacyCfg?.SkeletonLineThickness ?? cfg.Overlay.LineThickness;
		cfg.Overlay.LineOpacity = this._legacyCfg?.SkeletonLineOpacity ?? cfg.Overlay.LineOpacity;
		cfg.Overlay.LineOpacityUsing = this._legacyCfg?.SkeletonLineOpacityWhileUsing ?? cfg.Overlay.LineOpacityUsing;
		cfg.Overlay.DotRadius = this._legacyCfg?.SkeletonDotRadius ?? cfg.Overlay.DotRadius;

		// Gizmo
		cfg.Gizmo.AllowAxisFlip = this._legacyCfg?.AllowAxisFlip ?? cfg.Gizmo.AllowAxisFlip;

		// Autosave
		cfg.AutoSave.Enabled = this._legacyCfg?.EnableAutoSave ?? cfg.AutoSave.Enabled;
		cfg.AutoSave.Interval = this._legacyCfg?.AutoSaveInterval ?? cfg.AutoSave.Interval;
		cfg.AutoSave.Count = this._legacyCfg?.AutoSaveCount ?? cfg.AutoSave.Count;
		cfg.AutoSave.FilePath = this._legacyCfg?.AutoSavePath ?? cfg.AutoSave.FilePath;
		cfg.AutoSave.FolderFormat = this._legacyCfg?.AutoSaveFormat ?? cfg.AutoSave.FolderFormat;
		cfg.AutoSave.ClearOnExit = this._legacyCfg?.ClearAutoSavesOnExit ?? cfg.AutoSave.ClearOnExit;

		// Camera
		cfg.Editor.WorkcamMoveSpeed = this._legacyCfg?.FreecamMoveSpeed ?? cfg.Editor.WorkcamMoveSpeed;
		cfg.Editor.WorkcamSens = this._legacyCfg?.FreecamSensitivity ?? cfg.Editor.WorkcamSens;
		cfg.Editor.WorkcamFastMulti = this._legacyCfg?.FreecamShiftMuli ?? cfg.Editor.WorkcamFastMulti;
		cfg.Editor.WorkcamSlowMulti = this._legacyCfg?.FreecamCtrlMuli ?? cfg.Editor.WorkcamSlowMulti;
		cfg.Editor.WorkcamVertMulti = this._legacyCfg?.FreecamUpDownMuli ?? cfg.Editor.WorkcamVertMulti;

		
		// Keybinds
		if (this._legacyCfg?.FreecamForward != null)
			cfg.Keybinds.SetDefault("Camera_Work_Forward", MigrateKeybind(this._legacyCfg?.FreecamForward!));

		if (this._legacyCfg?.FreecamBack != null)
			cfg.Keybinds.SetDefault("Camera_Work_Back", MigrateKeybind(this._legacyCfg?.FreecamBack!));
		if (this._legacyCfg?.FreecamRight != null)
			cfg.Keybinds.SetDefault("Camera_Work_Right", MigrateKeybind(this._legacyCfg?.FreecamRight!));
		if (this._legacyCfg?.FreecamLeft != null)
			cfg.Keybinds.SetDefault("Camera_Work_Left", MigrateKeybind(this._legacyCfg?.FreecamLeft!));
		if (this._legacyCfg?.FreecamUp != null)
			cfg.Keybinds.SetDefault("Camera_Work_Up", MigrateKeybind(this._legacyCfg?.FreecamUp!));
		if (this._legacyCfg?.FreecamDown != null)
			cfg.Keybinds.SetDefault("Camera_Work_Down", MigrateKeybind(this._legacyCfg?.FreecamDown!));
		if (this._legacyCfg?.FreecamFast != null)
			cfg.Keybinds.SetDefault("Camera_Work_Fast",MigrateKeybind( this._legacyCfg?.FreecamFast!));
		if (this._legacyCfg?.FreecamSlow != null)
			cfg.Keybinds.SetDefault("Camera_Work_Slow", MigrateKeybind(this._legacyCfg?.FreecamSlow!));

		// KeyBinds dict
		if (this._legacyCfg?.KeyBinds.ContainsKey(LegacyConfig.Input.Purpose.SwitchToTranslate) ?? false)
			cfg.Keybinds.SetDefault("Gizmo_SetTranslateMode", MigrateKeys(this._legacyCfg?.KeyBinds[LegacyConfig.Input.Purpose.SwitchToTranslate]!));
		if (this._legacyCfg?.KeyBinds.ContainsKey(LegacyConfig.Input.Purpose.SwitchToRotate) ?? false)
			cfg.Keybinds.SetDefault("Gizmo_SetRotateMode", MigrateKeys(this._legacyCfg?.KeyBinds[LegacyConfig.Input.Purpose.SwitchToRotate]!));
		if (this._legacyCfg?.KeyBinds.ContainsKey(LegacyConfig.Input.Purpose.SwitchToScale) ?? false)
			cfg.Keybinds.SetDefault("Gizmo_SetScaleMode", MigrateKeys(this._legacyCfg?.KeyBinds[LegacyConfig.Input.Purpose.SwitchToScale]!));
		if (this._legacyCfg?.KeyBinds.ContainsKey(LegacyConfig.Input.Purpose.SwitchToUniversal) ?? false)
			cfg.Keybinds.SetDefault("Gizmo_SetUniversalMode", MigrateKeys(this._legacyCfg?.KeyBinds[LegacyConfig.Input.Purpose.SwitchToUniversal]!));
		if (this._legacyCfg?.KeyBinds.ContainsKey(LegacyConfig.Input.Purpose.ToggleLocalWorld) ?? false)
			cfg.Keybinds.SetDefault("Gizmo_ToggleMode", MigrateKeys(this._legacyCfg?.KeyBinds[LegacyConfig.Input.Purpose.ToggleLocalWorld]!));
		if (this._legacyCfg?.KeyBinds.ContainsKey(LegacyConfig.Input.Purpose.CircleThroughSiblingLinkModes) ?? false)
			cfg.Keybinds.SetDefault("Gizmo_MirrorRotation", MigrateKeys(this._legacyCfg?.KeyBinds[LegacyConfig.Input.Purpose.CircleThroughSiblingLinkModes]!));
		if (this._legacyCfg?.KeyBinds.ContainsKey(LegacyConfig.Input.Purpose.DeselectGizmo) ?? false)
			cfg.Keybinds.SetDefault("Select_None", MigrateKeys(this._legacyCfg?.KeyBinds[LegacyConfig.Input.Purpose.DeselectGizmo]!));
		if (this._legacyCfg?.KeyBinds.ContainsKey(LegacyConfig.Input.Purpose.NextCamera) ?? false)
			cfg.Keybinds.SetDefault("Camera_SetNext", MigrateKeys(this._legacyCfg?.KeyBinds[LegacyConfig.Input.Purpose.NextCamera]!));
		if (this._legacyCfg?.KeyBinds.ContainsKey(LegacyConfig.Input.Purpose.PreviousCamera) ?? false)
			cfg.Keybinds.SetDefault("Camera_SetPrevious", MigrateKeys(this._legacyCfg?.KeyBinds[LegacyConfig.Input.Purpose.PreviousCamera]!));
		if (this._legacyCfg?.KeyBinds.ContainsKey(LegacyConfig.Input.Purpose.ToggleFreeCam) ?? false)
			cfg.Keybinds.SetDefault("Camera_Work_Toggle", MigrateKeys(this._legacyCfg?.KeyBinds[LegacyConfig.Input.Purpose.ToggleFreeCam]!));

		if(this._legacyCfg?.BoneCategoryColors != null)
			foreach (var categoryColor in this._legacyCfg?.BoneCategoryColors!) {
				if (!this.LegacyCategoryMap.ContainsKey(categoryColor.Key)) continue;
				var color = ImGui.ColorConvertFloat4ToU32(categoryColor.Value);
				cfg.Categories.GetByName(this.LegacyCategoryMap[categoryColor.Key])?.BoneColor = color;
			}

		this._cfg.GenerateDefaultPresets(this._tempConfig);  //this doesnt happen because we set the version to 12 to avoid the check but we inadvertently blow the check up lol
		
		// Offsets
		if (this._legacyCfg?.CustomBoneOffset != null) {
			foreach (var dictPair in this._legacyCfg.CustomBoneOffset) {
				// convert Race_Gender to RaceSexId
				try {
					var convertedRaceSexId = this.LegacyRaceSexMap[dictPair.Key];
					cfg.Offsets.LoadLegacy(convertedRaceSexId, dictPair.Value);
				} catch (Exception e) {
					Ktisis.Log.Warning($"Could not deserialize legacy offsets from clipboard: {e}");
				}
			}
		}
		this._tempConfig = cfg;
	}


	private static ActionKeybind MigrateKeybind(LegacyConfig.Keybind keybind) {
		ActionKeybind bind = new ActionKeybind();
		
		foreach (var key in keybind.Keys) {
			if (KeyHelpers.IsModifierKey(key))
				bind.Combo.AddModifier(key);
			else
				bind.Combo.Key = key;
		}
		return bind;
	}

	private static ActionKeybind MigrateKeys(List<VirtualKey> keys) {
		ActionKeybind bind = new ActionKeybind();
		
		foreach (var key in keys) {
			if (KeyHelpers.IsModifierKey(key))
				bind.Combo.AddModifier(key);
			else
				bind.Combo.Key = key;
		}
		return bind;
	}

	internal void V3Skip() {
		var cfg = this._tempConfig;

		cfg.Keybinds.Enabled = true;
		cfg.Editor.ToggleOpenWindows = true;
	}

	// Begin from UI

	private bool _confirmed;

	public void Begin() {
		if (this._confirmed) return;
		this._confirmed = true;
		this._gpose.StateChanged -= this.OnGPoseStateChanged;
		this._tempConfig.Version = 12;
		this._cfg.File = this._tempConfig;
		this._cfg._isLoaded = true;
		this._cfg.Save();
		this._gpose.Reset();
		this.OnConfirmed?.Invoke();
	}
}
