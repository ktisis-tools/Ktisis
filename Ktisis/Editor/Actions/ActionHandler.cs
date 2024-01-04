namespace Ktisis.Editor.Actions;

public abstract class ActionHandler {
	protected readonly IActionManager Manager;
	
	protected ActionHandler(
		IActionManager manager
	) {
		this.Manager = manager;
	}
}
