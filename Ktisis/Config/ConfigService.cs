using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

using Dalamud.Logging;
using Dalamud.Plugin;

using Ktisis.Config.Display;
using Ktisis.Core.Impl;
using Ktisis.Core.Services;
using Ktisis.Data;

namespace Ktisis.Config; 

[KtisisService]
public class ConfigService {
	// Service
	
	private readonly DalamudPluginInterface _api;

	private readonly DataService _data;
	private readonly NotifyService _notify;

	public ConfigService(DalamudPluginInterface _api, DataService _data, NotifyService _notify) {
		this._api = _api;
		this._data = _data;
		this._notify = _notify;
	}
	
	// Config file & loading/creation

	public ConfigFile Config { get; private set; } = null!;
	
	private async Task<ConfigFile> CreateFromSchema() => new ConfigFile() {
		Categories = await this._data.ReadBoneCategories()
	};

	public async Task LoadConfig() {
		await Task.Yield();

		var timer = new Stopwatch();
		timer.Start();

		ConfigFile? cfg = null;

		try {
			var cfgBase = this._api.GetPluginConfig();
			cfg = cfgBase?.Version switch {
				// TODO: Legacy config upgrade
				not null => cfgBase as ConfigFile,
				_ => null
			};
		} catch (Exception err) {
			PluginLog.Error($"Error while loading configuration:\n{err}");
			
			this._notify.Error("Failed to load configuration. Please check your error log for more information.");
		}

		try {
			cfg ??= await CreateFromSchema();
		} catch (Exception err) {
			PluginLog.Error($"Error while creating configuration from schema:\n{err}");
			throw;
		}

		this.Config = cfg;

		timer.Stop();
		PluginLog.Debug($"Configuration loaded in {timer.Elapsed.TotalMilliseconds:0.000}ms");
	}

	public void SaveConfig() {
		try {
			this._api.SavePluginConfig(this.Config);
		} catch (Exception err) {
			PluginLog.Error($"Failed to save configuration:\n{err}");
			
			this._notify.Error("Failed to save configuration. Please check your error log for more information.");
		}
	}
	
	// Config helpers

	public ItemDisplay GetItemDisplay(ItemType type) => this.Config.Display!
		.GetValueOrDefault(type, new ItemDisplay());
}
