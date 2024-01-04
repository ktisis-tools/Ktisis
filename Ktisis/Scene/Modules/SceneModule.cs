using Ktisis.Interop.Hooking;

namespace Ktisis.Scene.Modules;

public abstract class SceneModule : HookModule {
	protected readonly ISceneManager Scene;

	public SceneModule(
		IHookMediator hook,
		ISceneManager scene
	) : base(hook) {
		this.Scene = scene;
	}

	protected bool CheckValid() {
		var valid = this.Scene.IsValid;
		if (!valid) {
			this.DisableAll();
			Ktisis.Log.Warning($"Hook called from '{this.GetType().Name}' with invalid scene state, disabling.");
		}
		return valid;
	}

	public virtual void Setup() { }

	public virtual void Update() { }
}
