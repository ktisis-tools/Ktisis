using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

using Dalamud.Configuration;
using Dalamud.Plugin;

using FFXIVClientStructs;

using Ktisis.Core.Attributes;
using Ktisis.Data.Config;
using Ktisis.Interface;
using Ktisis.Legacy.Interface;
using Ktisis.Localization;
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
	internal LegacyConfig.Configuration? _legacyCfg;
	public readonly LocaleManager Locale;
	
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
		if(v2)
			Ktisis.Log.Warning("User is migrating from Ktisis v0.2, activating legacy mode.");
		else 
			Ktisis.Log.Warning("User is migrating from Ktisis v0.2, activating legacy mode.");
		if (this.WasUserOnV2) {
			var configurations = new PluginConfigurations(new DirectoryInfo(this._dpi.GetPluginConfigDirectory()).Parent.ToString());
			this._legacyCfg = configurations?.LoadForType<LegacyConfig.Configuration>("Ktisis");
		}
		this._cfg.Load();
		this._gpose.StateChanged += this.OnGPoseStateChanged;
		this._gpose.Subscribe();
	}

	private void OnGPoseStateChanged(object sender, bool state) {
		if (!state || this._confirmed) return;
		var window = this._gui.GetOrCreate<MigratorWindow>(this, this._cfg);
		window.Open();
	}

	internal void MigrateConfig() {
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
