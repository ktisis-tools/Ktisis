using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Diagnostics;
using System.Linq;
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
			
			if (cfg is { Version: < 10 }) {
				cfg.Version = 10;
				this.MigrateSchema(cfg);
			}
			if (cfg is { Version: < 11 }) {
				cfg.Version = 11;
				this.GenerateDefaultPresets(cfg);
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
		Ktisis.Log.Debug("Updating category schema.");
		
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

	private void GenerateDefaultPresets(Configuration cfg) {
		var categories = SchemaReader.ReadCategories();

		var allPresetNames = categories.CategoryList.SelectMany(x => x.Presets).Distinct().ToList();
		Ktisis.Log.Info("All Presets: {0}", string.Join(", ", allPresetNames));
		var presets = allPresetNames.ToDictionary(
			x => x, 
			key => categories.CategoryList.Where(x => x.Presets.Contains(key)).SelectMany(x => x.Bones.Select(y => y.Name)).ToImmutableHashSet()
		);

		foreach (var (preset, bones) in presets)
		{
			cfg.Presets.Presets.TryAdd(preset, bones);
		}
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
