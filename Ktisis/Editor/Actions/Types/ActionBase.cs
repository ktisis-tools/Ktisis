using System.Reflection;

using Ktisis.Editor.Context;

namespace Ktisis.Editor.Actions.Types;

public abstract class ActionBase {
	protected IActionManager Manager;

	protected IEditorContext Context => this.Manager.Context;
	
	protected ActionBase(
		IActionManager manager
	) {
		this.Manager = manager;
	}

	public string GetName() => this.GetAttribute().Name;
	
	public ActionAttribute GetAttribute() => this.GetType()
		.GetCustomAttribute<ActionAttribute>()!;
	
	public abstract bool Invoke();
}
