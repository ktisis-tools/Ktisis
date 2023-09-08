using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Dalamud.Logging;
using Dalamud.Plugin;

using Ktisis.Data.Config.Display;

namespace Ktisis.Data.Config; 

public class ConfigService {
	// Service
	
	private readonly DalamudPluginInterface _api;

	private readonly DataService _data;

	public ConfigService(DalamudPluginInterface _api, DataService _data) {
		this._api = _api;
		this._data = _data;
	}
	
	// Config file & loading/creation

	public ConfigFile Config { get; private set; } = null!;

	public async Task LoadConfig() {
		await Task.Yield();

		ConfigFile? cfg = null;

		try {
			var cfgBase = this._api.GetPluginConfig();
			cfg = cfgBase?.Version switch {
				// TODO: Legacy config upgrade
				not null => cfgBase as ConfigFile,
				_ => null
			};
		} catch (Exception err) {
			PluginLog.Error($"Error while reading saved configuration:\n{err}");
		}

		try {
			cfg ??= await CreateFromSchema();
		} catch (Exception err) {
			PluginLog.Error($"Error while creating configuration from schema:\n{err}");
			throw;
		}

		this.Config = cfg;
	}

	private async Task<ConfigFile> CreateFromSchema() => new ConfigFile() {
		Categories = await this._data.ReadBoneCategories()
	};
	
	// Config helpers

	public ItemDisplay GetItemDisplay(ItemType type) => this.Config.Display!
		.GetValueOrDefault(type, new ItemDisplay());
}
