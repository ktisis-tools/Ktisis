using System.Reflection;

namespace Ktisis.Actions.Impl; 

public interface IAction {
	public bool CanInvoke() => true;
	
	public bool Invoke();

	public ActionAttribute GetAttribute() => GetType()
		.GetCustomAttribute<ActionAttribute>()!;

	public string GetName() => GetAttribute().Name;
}
