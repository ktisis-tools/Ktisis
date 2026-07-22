using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;

using Dalamud.Plugin;

using Ktisis.Core.Attributes;
using Ktisis.Data.Config;
using Ktisis.Data.Config.Bones;
using Ktisis.Editor.Posing.Types;

namespace Ktisis.Localization;

[Singleton]
public class LocaleManager : IDisposable {
	// Service
	
	private ConfigManager? _cfg;
	private readonly IDalamudPluginInterface _dpi;
	
	private readonly LocaleDataLoader Loader = new();

	private static Dictionary<string, string> localeFallbackMap = new() {
		{ "zh_SG", "zh_CN" },
		{ "zh_MO", "zh_TW" },
		{ "zh_HK", "zh_TW" },
	};

	public List<LocaleMetaData> AvailableLocales = new();
	public LocaleData? Data;
	public LocaleData? FallbackData;

	internal delegate void LocaleChange();
	internal event LocaleChange LocaleChanged;
	
	public LocaleManager(
		IDalamudPluginInterface dpi
	) {
		this._dpi = dpi;
	}

	private Configuration? config => this._cfg._isLoaded ? this._cfg.File : null;

	public void Initialize(ConfigManager cfg) {
		this._cfg = cfg;
		foreach (var resource in Assembly.GetExecutingAssembly().GetManifestResourceNames().Where(s => s.StartsWith("Ktisis.Localization.Data"))) {
			if(this.AvailableLocales.All(l => l.TechnicalName != resource.Split('.')[3]))
				this.AvailableLocales.Add(this.Loader.LoadMeta(resource.Split('.')[3]));
		}
		cfg.WithConfigLoaded(config => {
			if(config.Locale.AutoDetect) {
				this.HandleLanguageChangeDelegate();
				/* (n.b. the above method will call LanguageChanged immediately) */
			} else {
				/* we need to check for available locales here in case anyone has an old configuration that names an unavailable locale */
				string targetLocale = this.GetBestAvailableLocale(config.Locale.LocaleId) ?? "en_US";
				this.LoadLocale(targetLocale);
				/* (n.b. we don't write the locale back to the config file in case the user's preferred locale becomes available again in a new version) */

				if(targetLocale != "en_US")
					this.LoadFallbackLocale();
			}
		});
	}

	public void HandleLanguageChangeDelegate() {
		this._dpi.LanguageChanged -= this.LanguageChanged;
		if (this.config?.Locale.AutoDetect ?? false) {
			this.LanguageChanged(this._dpi.UiLanguage);
			this._dpi.LanguageChanged += this.LanguageChanged;
		}
	}
	public void LanguageChanged(string uiLanguage) {
		//TODO: Check for default names on Camera and Actors to repopulate
		if(this.config is {} config) {
			/* Sanity check */
			if(!config.Locale.AutoDetect) return;
			string envLocale = uiLanguage + "_" + RegionInfo.CurrentRegion.TwoLetterISORegionName;
			string targetLocale = this.GetBestAvailableLocale(envLocale) ?? "en_US";
			config.Locale.LocaleId = targetLocale;
			this.LoadLocale(targetLocale);
		}
	}

	private string? GetBestAvailableLocale(string inputLocale) {
		var availableLocales = this.AvailableLocales.Select(x => x.TechnicalName).ToHashSet();
		if(availableLocales.Contains(inputLocale)) return inputLocale;
		if(localeFallbackMap.TryGetValue(inputLocale, out var remappedLocale)) {
			if(availableLocales.Contains(inputLocale)) return remappedLocale;
		}
		var languageMatch = inputLocale.Split("_")[0] + "_";
		var languageFallback = this.AvailableLocales.FirstOrDefault(x => x.TechnicalName.StartsWith(languageMatch));
		if(languageFallback != null) return languageFallback.TechnicalName;
		return null;
	}


	// Localization methods

	public string Translate(string handle, Dictionary<string, string>? parameters = null) {
		return this.Data?.Translate(handle, parameters) ?? (this.FallbackData?.Translate(handle, parameters) ?? handle);
	}
	
	public bool HasTranslationFor(string handle) {
		return this.Data?.HasTranslationFor(handle) ?? false;
	}

	public void LoadLocale(string technicalName) {
		Ktisis.Log.Verbose($"Reading localization file for '{technicalName}'");
		if (this.Data == null || this.Data.MetaData.TechnicalName != technicalName) {
			this.Data = this.Loader.LoadData(technicalName);
			if (technicalName != "en_US")
				LoadFallbackLocale();
			else
				this.FallbackData = null;
			LocaleChanged?.Invoke();
		}
			
	}
	
	public void LoadFallbackLocale() {
		Ktisis.Log.Verbose($"FALLBACK - Reading localization file for 'en_US'");
		if (this.FallbackData == null || this.FallbackData.MetaData.TechnicalName != "en_US")
			this.FallbackData = this.Loader.LoadData("en_US");
	}
	// Helpers
	
	public string GetBoneName(PartialBoneInfo bone) => this.GetBoneName(bone.Name);

	public string GetBoneName(string name) {
		var key = $"bone.{name}";
		var friendly_bone_names = this.config?.Categories.ShowFriendlyBoneNames ?? false;
		return friendly_bone_names && this.HasTranslationFor(key) ? this.Translate(key) : name;
	}

	public string GetCategoryName(BoneCategory category) {
		var key = $"boneCategory.{category.Name}";
		return this.HasTranslationFor(key) ? this.Translate(key) : category.Name;
	}

	public int RandomHintKey() {
		var count = this.Data?.KeysMatchingPrefix("hints.");
		if (count == null) return 0;

		var r = new Random();
		return r.Next(0, count.Value) + 1;
	}

	public void Dispose() {
			this._dpi.LanguageChanged -= this.LanguageChanged;
  }
}
