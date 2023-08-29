using Ktisis.Data;
using Ktisis.Data.Config;

namespace Ktisis.Scene; 

public class SceneContext {
	public readonly SceneGraph Scene;
	
	private readonly DataService _data;

	public SceneContext(SceneGraph scene, DataService _data) {
		this.Scene = scene;
		
		this._data = _data;
	}
	
	// Config

	public ConfigFile GetConfig() => this._data.GetConfig();
}