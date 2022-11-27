using System;
using System.Collections.Generic;
using System.Linq;

namespace Ktisis.Localization {
	public class LocaleData {
		private readonly Dictionary<string, string> _translationData;

		public LocaleMetaData MetaData { get; }

		public LocaleData(LocaleMetaData metaData, Dictionary<string, string> translationData) {
			_translationData = translationData;
			MetaData = metaData;
		}

		public string Translate(string key) {
			/* TODO: Implementing some form of fallback system might be good here. */
			return _translationData.TryGetValue(key, out string? translated) ? translated : key;
		}

		public bool HasTranslationFor(string key) {
			return _translationData.ContainsKey(key);
		}
	}
}
