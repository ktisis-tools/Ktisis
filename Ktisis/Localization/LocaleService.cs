using System.Collections.Generic;

using Dalamud.Logging;

using Ktisis.Core.Impl;
using Ktisis.Data.Config;

namespace Ktisis.Localization;

[KtisisService]
public class LocaleService : IServiceInit {
	// Service

	private readonly ConfigService _cfg;
	
	private readonly LocaleDataLoader Loader = new();
	
	private LocaleData? Data;

	public LocaleService(ConfigService _cfg) {
		this._cfg = _cfg;
	}

	public void Initialize() {
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
		PluginLog.Verbose($"Reading localization file for '{technicalName}'");
		if (this.Data == null || this.Data.MetaData.TechnicalName != technicalName)
			this.Data = this.Loader.LoadData(technicalName);
	}
}
