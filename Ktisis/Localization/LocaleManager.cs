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

	public List<LocaleMetaData> AvailableLocales = new();
	public LocaleData? Data;
	public LocaleData? FallbackData;

	public LocaleManager(
		IDalamudPluginInterface dpi
	) {
		this._dpi = dpi;
	}

	public void Initialize(ConfigManager cfg) {
		this._cfg = cfg;
		// TODO: Listen for locale changes.
		this.HandleLanguageChangeDelegate();
		foreach (var resource in Assembly.GetExecutingAssembly().GetManifestResourceNames().Where(s => s.StartsWith("Ktisis.Localization.Data"))) {
			if(this.AvailableLocales.All(l => l.TechnicalName != resource.Split('.')[3]))
				this.AvailableLocales.Add(this.Loader.LoadMeta(resource.Split('.')[3]));
		}
		this.LoadLocale(this._cfg.File.Locale.LocaleId);
		if(this._cfg.File.Locale.LocaleId != "en_US")
			this.LoadFallbackLocale();
	}

	public void HandleLanguageChangeDelegate() {
		this._dpi.LanguageChanged -= this.LanguageChanged;
		if (this._cfg.File.Locale.AutoDetect) {
			this._dpi.LanguageChanged += this.LanguageChanged;
		}
	}
	public void LanguageChanged(string lang) {
		//TODO: Check for default names on Camera and Actors to repopulate
		var localeFile = lang + "_" + RegionInfo.CurrentRegion.TwoLetterISORegionName;
		if (this.HasTranslationFor(localeFile)) {
			this._cfg.File.Locale.LocaleId = localeFile;
			this.LoadLocale(localeFile);
		}
			
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
		var friendly_bone_names = this._cfg.File.Categories.ShowFriendlyBoneNames;
		return friendly_bone_names && this.HasTranslationFor(key) ? this.Translate(key) : name;
	}

	public string GetCategoryName(BoneCategory category) {
		var key = $"boneCategory.{category.Name}";
		return this.HasTranslationFor(key) ? this.Translate(key) : category.Name;
	}
	

	public void Dispose() {
			this._dpi.LanguageChanged -= this.LanguageChanged;
	}
}
