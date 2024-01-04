using System;
using System.Diagnostics;

using Dalamud.Plugin;

using Ktisis.Core.Attributes;

namespace Ktisis.Data.Config;

[Singleton]
public class ConfigManager : IDisposable {
	private readonly DalamudPluginInterface _dpi;
	private readonly SchemaReader _schema;
	
	public Configuration Config { get; private set; } = null!;

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
			var cfgBase = this._dpi.GetPluginConfig();
			cfg = cfgBase?.Version switch {
				// TODO: Legacy config upgrade
				not null => cfgBase as Configuration,
				_ => null
			};
		} catch (Exception err) {
			Ktisis.Log.Error($"Failed to load configuration:\n{err}");
		}

		try {
			cfg ??= this.CreateDefault();
		} catch (Exception err) {
			Ktisis.Log.Error($"Failed to create default configuration:\n{err}");
			throw;
		}

		this.Config = cfg;
		
		timer.Stop();
		Ktisis.Log.Debug($"Configuration loaded in {timer.Elapsed.TotalMilliseconds:0.00}ms");
	}

	public void Save() {
		try {
			this._dpi.SavePluginConfig(this.Config);
		} catch (Exception err) {
			Ktisis.Log.Error($"Failed to save configuration:\n{err}");
		}
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
