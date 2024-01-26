using System.Reflection;

using Ktisis.Core.Types;

namespace Ktisis.Actions.Types;

public abstract class ActionBase {
	protected IPluginContext Context { get; }
	
	protected ActionBase(IPluginContext ctx) {
		this.Context = ctx;
	}
	
	public string GetName() => this.GetAttribute().Name;
	
	public ActionAttribute GetAttribute() => this.GetType()
		.GetCustomAttribute<ActionAttribute>()!;

	public virtual bool CanInvoke() => true;
	
	public abstract bool Invoke();
}
