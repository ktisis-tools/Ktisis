using System.Collections.Generic;

using Ktisis.Core.Attributes;
using Ktisis.Data.Config;
using Ktisis.Data.Config.Bones;
using Ktisis.Editor.Posing.Types;

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
		this.LoadLocale(this._cfg.File.Locale.LocaleId);
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
}
