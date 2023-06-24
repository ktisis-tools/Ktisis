using System;

namespace Ktisis.Events.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class ListenerAttribute : Attribute {
	public readonly Type DelegateType;
	protected ListenerAttribute(Type t) => DelegateType = t;
}

public class ListenerAttribute<T> : ListenerAttribute where T : Delegate {
	public ListenerAttribute() : base(typeof(T)) { }
}
