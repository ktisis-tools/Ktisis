namespace Ktisis.Scenes.Managers;

public abstract class ManagerBase {
	protected readonly Scene Scene;

	protected ManagerBase(Scene scene) {
		Scene = scene;
	}
}
