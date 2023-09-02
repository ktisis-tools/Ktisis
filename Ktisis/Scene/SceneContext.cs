using Ktisis.Data;
using Ktisis.Data.Config;

namespace Ktisis.Scene;

public enum EditMode {
	None,
	Object,
	Pose
}

public class SceneContext {
	private readonly SceneGraph Scene;
	
	private readonly DataService _data;

	public SceneContext(SceneGraph scene, DataService _data) {
		this.Scene = scene;
		
		this._data = _data;
	}
	
	// Editing

	public EditMode EditMode = EditMode.Object;
	
	// Handler access

	public T GetHandler<T>() => this.Scene.GetHandler<T>();
	
	// Config

	public ConfigFile GetConfig() => this._data.GetConfig();
}