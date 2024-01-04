using System.Collections.Generic;

using Ktisis.Core.Attributes;
using Ktisis.Data.Config;
using Ktisis.Data.Config.Bones;
using Ktisis.Editor.Posing.Partials;
using Ktisis.Scene.Entities.Skeleton;

namespace Ktisis.Localization;

[Singleton]
public class LocaleManager {
	// Service

	private readonly ConfigManager _cfg;
	
	private readonly LocaleDataLoader Loader = new();
	
	private LocaleData? Data;

	public LocaleManager(
		ConfigManager cfg
	) {
		this._cfg = cfg;
	}

	public void Initialize() {
		// TODO: Listen for locale changes.
		this.LoadLocale(this._cfg.Config.Locale.LocaleId);
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
	
	// Helpers

	public string GetBoneName(PartialBoneInfo bone) {
		var key = $"bone.{bone.Name}";
		return this.HasTranslationFor(key) ? this.Translate(key) : bone.Name;
	}

	public string GetCategoryName(BoneCategory category) {
		var key = $"boneCategory.{category.Name}";
		return this.HasTranslationFor(key) ? this.Translate(key) : category.Name;
	}
}
