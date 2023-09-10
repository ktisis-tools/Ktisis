using System.Collections.Generic;

using Ktisis.Core.Impl;

namespace Ktisis.Localization;

[KtisisService]
public class LocaleService {
	// Fields
	
	private LocaleData? Data;

	private readonly LocaleDataLoader Loader = new();
	
	// Localization methods

	public string Translate(string handle, Dictionary<string, string>? parameters = null) {
		return this.Data?.Translate(handle, parameters) ?? handle;
	}

	public bool HasTranslationFor(string handle) {
		return this.Data?.HasTranslationFor(handle) ?? false;
	}

	public void LoadLocale(string technicalName) {
		// ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
		if (this.Data == null || this.Data.MetaData.TechnicalName != technicalName)
			this.Data = this.Loader.LoadData(technicalName);
	}
}
