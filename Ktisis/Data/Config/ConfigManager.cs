using System;
using System.IO;
using System.Diagnostics;

using Dalamud.Plugin;

using Newtonsoft.Json;

using Ktisis.Core.Attributes;

namespace Ktisis.Data.Config;

[Singleton]
public class ConfigManager : IDisposable {
	private readonly DalamudPluginInterface _dpi;
	private readonly SchemaReader _schema;

	private bool _isLoaded;
	public Configuration File { get; private set; } = null!;

	public ConfigManager(
		DalamudPluginInterface dpi,
		SchemaReader schema
	) {
		this._dpi = dpi;
		this._schema = schema;
	}
	
	// Load & Save

	public void Load() {
		var timer = new Stopwatch();
		timer.Start();

		Configuration? cfg = null;

		try {
			// TODO: Legacy migration
			cfg = this.OpenConfigFile();

			if (cfg is { Version: < 6 })
				cfg.Categories = this._schema.ReadCategories();
		} catch (Exception err) {
			Ktisis.Log.Error($"Failed to load configuration:\n{err}");
		}

		try {
			cfg ??= this.CreateDefault();
		} catch (Exception err) {
			Ktisis.Log.Error($"Failed to create default configuration:\n{err}");
			throw;
		}

		this.File = cfg;
		this._isLoaded = true;
		
		timer.Stop();
		Ktisis.Log.Debug($"Configuration loaded in {timer.Elapsed.TotalMilliseconds:0.00}ms");
	}

	public void Save() {
		try {
			if (this._isLoaded)
				this.SaveConfigFile();
		} catch (Exception err) {
			Ktisis.Log.Error($"Failed to save configuration:\n{err}");
		}
	}
	
	// TEMPORARY: v3 config file

	public bool GetConfigFileExists() {
		var path = this.GetConfigFilePath();
		return Path.Exists(path);
	}

	private Configuration? OpenConfigFile() {
		var path = this.GetConfigFilePath();
		if (!Path.Exists(path)) return null;

		var content = System.IO.File.ReadAllText(path);
		return JsonConvert.DeserializeObject<Configuration>(content);
	}

	private void SaveConfigFile() {
		var path = this.GetConfigFilePath();
		var content = JsonConvert.SerializeObject(this.File, Formatting.Indented);
		System.IO.File.WriteAllText(path, content);
	}

	private string GetConfigFilePath() {
		return Path.Join(this._dpi.GetPluginConfigDirectory(), "KtisisV3.json");
	}
	
	// Create default config

	private Configuration CreateDefault() {
		return new Configuration {
			Categories = this._schema.ReadCategories()
		};
	}
	
	// IDisposable

	public void Dispose() => this.Save();
}
