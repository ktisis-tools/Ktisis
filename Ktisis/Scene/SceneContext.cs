using Ktisis.Data;
using Ktisis.Data.Config;

namespace Ktisis.Scene;

public class SceneContext {
	// Constructor
	
	private readonly ConfigService _cfg;

	private readonly SceneManager Manager;

	public SceneContext(SceneManager manager, ConfigService _cfg) {
		this._cfg = _cfg;
		
		this.Manager = manager;
	}
	
	// Scene + handler access

	public SceneGraph? Scene => this.Manager.Scene;

	public T GetHandler<T>() => this.Manager.GetHandler<T>();
	
	// Config

	public ConfigFile GetConfig() => this._cfg.Config;
}
