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

	// Handler access

	public T GetHandler<T>() => this.Scene.GetHandler<T>();

	// Config

	public ConfigFile GetConfig() => this._data.GetConfig();
}
