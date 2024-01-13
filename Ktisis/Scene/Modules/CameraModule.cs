using Ktisis.Interop.Hooking;

namespace Ktisis.Scene.Modules;

public class CameraModule : SceneModule {
	public CameraModule(
		IHookMediator hook,
		ISceneManager scene
	) : base(hook, scene) { }

	public override void Setup() {
		
	}
}
