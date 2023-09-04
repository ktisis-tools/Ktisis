using Ktisis.Data;
using Ktisis.Data.Config;

namespace Ktisis.Scene;

public class SceneContext {
	// Constructor
	
	private readonly DataService _data;

	public SceneContext(DataService _data) {
		this._data = _data;
	}
	
	// Config

	public ConfigFile GetConfig() => this._data.GetConfig();
}