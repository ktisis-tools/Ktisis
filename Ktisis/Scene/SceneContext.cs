using Ktisis.Config;
using Ktisis.Config.Bones;
using Ktisis.Localization;
using Ktisis.Posing.Bones;

namespace Ktisis.Scene;

public class SceneContext {
	// Constructor
	
	private readonly SceneManager Manager;
	
	private readonly ConfigService _cfg;
	private readonly LocaleService _locale;

	public SceneContext(
		SceneManager manager,
		ConfigService _cfg,
		LocaleService _locale
	) {
		this.Manager = manager;
		
		this._cfg = _cfg;
		this._locale = _locale;
	}
	
	// Scene + handler access

	public SceneGraph? Scene => this.Manager.Scene;

	public T GetHandler<T>() => this.Manager.GetHandler<T>();
	
	// Config

	public ConfigFile GetConfig() => this._cfg.Config;
	
	// Locale

	public string GetBoneName(BoneData bone) {
		var key = $"bone.{bone.Name}";
		return this._locale.HasTranslationFor(key) ? this._locale.Translate(key) : bone.Name;
	}

	public string GetCategoryName(BoneCategory cat) {
		var key = $"boneCategory.{cat.Name}";
		return this._locale.HasTranslationFor(key) ? this._locale.Translate(key) : cat.Name ?? "UNKNOWN";
	}
}
