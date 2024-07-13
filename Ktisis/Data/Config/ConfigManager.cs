using System;
using System.IO;
using System.Diagnostics;

using Dalamud.Plugin;

using Newtonsoft.Json;

using Ktisis.Core.Attributes;
using Ktisis.Data.Serialization;

namespace Ktisis.Data.Config;

public delegate void OnConfigSaved(Configuration cfg);

[Singleton]
public class ConfigManager : IDisposable {
	private readonly IDalamudPluginInterface _dpi;

	private bool _isLoaded;
	public Configuration File { get; private set; } = null!;

	public event OnConfigSaved? OnSaved;

	public ConfigManager(
		IDalamudPluginInterface dpi
	) {
		this._dpi = dpi;
	}
	
	// Load & Save

	public void Load() {
		var timer = new Stopwatch();
		timer.Start();

		Configuration? cfg = null;

		try {
			// TODO: Legacy migration
			cfg = this.OpenConfigFile();

			if (cfg is { Version: < 9 }) {
				cfg.Version = 9;
				this.MigrateSchema(cfg);
			}
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
		if (!this._isLoaded) return;
		
		try {
			this.SaveConfigFile();
			if (!this._isDisposing)
				this.OnSaved?.Invoke(this.File);
		} catch (Exception err) {
			Ktisis.Log.Error($"Failed to save configuration:\n{err}");
		}
	}
	
	// Schema migration

	private void MigrateSchema(Configuration cfg) {
		var categories = SchemaReader.ReadCategories();
		
		foreach (var cat in categories.CategoryList) {
			var prev = cfg.Categories.GetByName(cat.Name);
			if (prev == null) continue;
			cat.BoneColor = prev.BoneColor;
			cat.GroupColor = prev.GroupColor;
			cat.LinkedColors = prev.LinkedColors;
		}

		cfg.Categories = categories;
	}
	
	// TEMPORARY: v3 config file

	public bool GetConfigFileExists() {
		var path = this.GetConfigFilePath();
		return Path.Exists(path);
	}

	private Configuration? OpenConfigFile() {
		Ktisis.Log.Verbose("Loading configuration...");
		
		var path = this.GetConfigFilePath();
		if (!Path.Exists(path)) return null;

		var content = System.IO.File.ReadAllText(path);
		return JsonConvert.DeserializeObject<Configuration>(content);
	}

	private void SaveConfigFile() {
		Ktisis.Log.Verbose("Saving configuration...");
		
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
			Categories = SchemaReader.ReadCategories()
		};
	}
	
	// IDisposable

	private bool _isDisposing;

	public void Dispose() {
		this._isDisposing = true;
		this.Save();
		GC.SuppressFinalize(this);
	}
}
