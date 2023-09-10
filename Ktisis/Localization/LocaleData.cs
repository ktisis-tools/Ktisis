using System.Text;
using System.Collections.Generic;

namespace Ktisis.Localization; 

public class LocaleData {
	private readonly Dictionary<string, string> _translationData;

	public LocaleMetaData MetaData { get; }

	public LocaleData(LocaleMetaData metaData, Dictionary<string, string> translationData) {
		this._translationData = translationData;
		this.MetaData = metaData;
	}

	public string Translate(string key, Dictionary<string, string>? parameters = null) {
		/* TODO: Implementing some form of fallback system might be good here. */
		if(!this._translationData.TryGetValue(key, out string? translationString)) {
			// Disabling this due to per-frame log spam.
			// Logger.Warning("Unassigned translation key '{0}' for locale '{1}'", key, this.MetaData.TechnicalName);
			return key;
		}
		return ReplaceParameters(key, translationString, parameters);
	}

	public bool HasTranslationFor(string key) {
		return this._translationData.ContainsKey(key);
	}

	private string ReplaceParameters(string handle, string translationString, Dictionary<string, string>? parameters) {
		StringBuilder result = new(translationString.Length);
		StringBuilder key = new(16);
		bool inParameter = false;
		foreach(char c in translationString) {
			if(!inParameter) {
				if(c == '%')
					inParameter = true;
				else
					result.Append(c);
			} else {
				if(c == '%') {
					if(key.Length == 0) /* '%%' escape sequence */
						result.Append('%');
					else {
						string keyV = key.ToString();
						string? value = null;
						parameters?.TryGetValue(keyV, out value);
						if(value == null) {
							Logger.Warning("Unassigned parameter '{0}' in value for '{1}' in locale '{2}'", keyV, handle, this.MetaData.TechnicalName);
							value = $"%{keyV}%";
						}
						result.Append(value);
						key.Clear();
					}
					
					inParameter = false;
				} else {
					key.Append(c);
				}
			}
		}
		return result.ToString();
	}
}
