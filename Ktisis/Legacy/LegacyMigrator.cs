using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

using Dalamud.Configuration;
using Dalamud.Plugin;

using Ktisis.Core.Attributes;
using Ktisis.Data.Config;
using Ktisis.Interface;
using Ktisis.Legacy.Interface;
using Ktisis.Services;
using Ktisis.Services.Game;

using Microsoft.Extensions.DependencyInjection;

namespace Ktisis.Legacy;

[Singleton]
public class LegacyMigrator {
	private readonly GPoseService _gpose;
	private readonly GuiManager _gui;
	private readonly IDalamudPluginInterface _dpi;
	private readonly ConfigManager _cfg;

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
		Ktisis.Log.Warning("User is migrating from Ktisis v0.2, activating legacy mode.");
		this._gpose.StateChanged += this.OnGPoseStateChanged;
		this._gpose.Subscribe();
		this.MigrateConfig();
	}

	private void OnGPoseStateChanged(object sender, bool state) {
		if (!state || this._confirmed) return;
		var window = this._gui.GetOrCreate<MigratorWindow>(this);
		window.Open();
	}

	private void MigrateConfig() {
		var configurations = new PluginConfigurations(new DirectoryInfo(this._dpi.GetPluginConfigDirectory()).Parent.ToString());
		var legacyConfig = configurations?.LoadForType<LegacyConfig.Configuration>("Ktisis");

		if (legacyConfig != null) {
			this._cfg.Load();
			if (this._cfg.GetConfigFileExists()) {
				this._cfg.File.AutoSave.Enabled = legacyConfig.EnableAutoSave ?? this._cfg.File.AutoSave.Enabled;
				this._cfg.File.Editor.IncognitoPlayerNames = !legacyConfig.DisplayCharName ?? this._cfg.File.Editor.IncognitoPlayerNames;
				this._cfg.File.Categories.ShowNsfwBones = !legacyConfig.CensorNsfw ?? this._cfg.File.Categories.ShowNsfwBones;
			}
		}
	}


	// Begin from UI

	private bool _confirmed;
	
	public void Begin() {
		if (this._confirmed) return;
		this._confirmed = true;
		this._gpose.StateChanged -= this.OnGPoseStateChanged;
		this._gpose.Reset();
		this.OnConfirmed?.Invoke();
	}
}
