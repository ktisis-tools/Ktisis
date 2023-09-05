using Ktisis.Data;
using Ktisis.Data.Config;

namespace Ktisis.Scene;

public class SceneContext {
	// Constructor
	
	private readonly DataService _data;

	private readonly SceneManager Manager;

	public SceneContext(SceneManager manager, DataService _data) {
        this._data = _data;
        
        this.Manager = manager;
	}
	
	// Scene + handler access

	public SceneGraph? Scene => this.Manager.Scene;

	public T GetHandler<T>() => this.Manager.GetHandler<T>();
	
	// Config

	public ConfigFile GetConfig() => this._data.GetConfig();
}
