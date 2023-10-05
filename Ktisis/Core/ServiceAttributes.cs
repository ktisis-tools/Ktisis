using System;

namespace Ktisis.Core;

// GlobalService

[Flags]
public enum ServiceFlags {
	None = 0
}

[AttributeUsage(AttributeTargets.Class)]
public class DIServiceAttribute : Attribute {
	// Properties
	
	public readonly ServiceFlags Flags;
	
	// Constructor
	
	public DIServiceAttribute() {}

	public DIServiceAttribute(ServiceFlags flags)
		=> this.Flags = flags;
	
	// Methods

	public bool HasFlag(ServiceFlags flag)
		=> this.Flags.HasFlag(flag);
}

// Component

[AttributeUsage(AttributeTargets.Class)]
public class DIComponentAttribute : Attribute { }

// Event

[AttributeUsage(AttributeTargets.Class)]
public class DIEventAttribute : Attribute { }
