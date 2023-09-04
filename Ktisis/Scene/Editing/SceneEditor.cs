namespace Ktisis.Scene.Editing;

public enum EditMode {
	None,
	Object,
	Pose
}

public class SceneEditor {
	// Constructor
	
	private readonly SceneManager _manager;

	public readonly SelectState Selection;
	
	public SceneEditor(SceneManager _manager) {
		this._manager = _manager;
		this.Selection = new SelectState();
		
		_manager.OnSceneChanged += OnSceneChanged;
	}
	
	// Editor state

	public EditMode CurrentMode;
	
	// Events

	private void OnSceneChanged(SceneGraph? scene) {
		if (scene is not null)
			this.Selection.Attach(scene);
		else
			this.Selection.Clear();
	}
}