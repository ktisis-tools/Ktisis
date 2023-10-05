using System.Collections.Generic;

using Ktisis.Core;
using Ktisis.Data.Config;
using Ktisis.Events;

namespace Ktisis.Interface.Localization;

[DIService]
public class LocaleService {
	// Service

	private readonly ConfigService _cfg;
	
	private readonly LocaleDataLoader Loader = new();
	
	private LocaleData? Data;

	public LocaleService(
		ConfigService _cfg,
		InitEvent _init
	) {
		this._cfg = _cfg;
		
		_init.Subscribe(Initialize);
	}

	private void Initialize() {
		// TODO: Listen for locale changes.
		LoadLocale(this._cfg.Config.LocaleId);
	}
	
	// Localization methods

	public string Translate(string handle, Dictionary<string, string>? parameters = null) {
		return this.Data?.Translate(handle, parameters) ?? handle;
	}

	public bool HasTranslationFor(string handle) {
		return this.Data?.HasTranslationFor(handle) ?? false;
	}

	public void LoadLocale(string technicalName) {
		Ktisis.Log.Verbose($"Reading localization file for '{technicalName}'");
		if (this.Data == null || this.Data.MetaData.TechnicalName != technicalName)
			this.Data = this.Loader.LoadData(technicalName);
	}
}
