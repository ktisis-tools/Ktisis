using System;
using System.IO;

using Dalamud.Configuration;
using Dalamud.Plugin;

using Ktisis.Core.Attributes;
using Ktisis.Data.Config;
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
	private readonly LocaleManager Locale;

	public bool WasUserOnV2;

	public event Action? OnConfirmed;

	public LegacyMigrator(
		GPoseService gpose,
		GuiManager gui,
		IDalamudPluginInterface dpi,
		ConfigManager cfg,
		LocaleManager localeManager
	) {
		this._gpose = gpose;
		this._gui = gui;
		this._dpi = dpi;
		this._cfg = cfg;
		this.Locale = localeManager;
	}

	// Setup

	public void Setup(bool v2 = true) {
		this.WasUserOnV2 = v2;
		this._cfg.Load();
		this.Locale.Initialize();
		if (v2) {
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
		var window = this._gui.GetOrCreate<MigratorWindow>(this, this._cfg, this.Locale);
		window.Open();
	}

	internal void MigrateConfig() {
		var cfg = this._cfg.File;

		// The big 3 so to speak
		cfg.Editor.IncognitoPlayerNames = this._legacyCfg?.DisplayCharName ?? cfg.Editor.IncognitoPlayerNames;
		cfg.Categories.ShowNsfwBones = !this._legacyCfg?.CensorNsfw ?? cfg.Categories.ShowNsfwBones;
		cfg.Keybinds.Enabled = this._legacyCfg?.EnableKeybinds ?? cfg.Keybinds.Enabled;


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
		// ngl fuck this

		// Offsets TODO:Validate
		cfg.Offsets.BoneOffsets = this._legacyCfg?.CustomBoneOffset ?? cfg.Offsets.BoneOffsets;

	}

	internal void V3Skip() {
		var cfg = this._cfg.File;

		cfg.Keybinds.Enabled = true;
		cfg.Editor.ToggleOpenWindows = true;
	}

	// Begin from UI

	private bool _confirmed;

	public void Begin() {
		if (this._confirmed) return;
		this._confirmed = true;
		this._gpose.StateChanged -= this.OnGPoseStateChanged;
		this._cfg.File.Version = 12;
		this._gpose.Reset();
		this.OnConfirmed?.Invoke();
	}
}
