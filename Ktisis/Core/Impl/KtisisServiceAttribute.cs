using System;

namespace Ktisis.Core.Impl;

[Flags]
public enum ServiceFlags {
	None = 0
}

public class KtisisServiceAttribute : Attribute {
	// Properties
	
	public readonly ServiceFlags Flags;
	
	// Constructor
	
	public KtisisServiceAttribute() {}

	public KtisisServiceAttribute(ServiceFlags flags)
		=> this.Flags = flags;
	
	// Methods

	public bool HasFlag(ServiceFlags flag)
		=> this.Flags.HasFlag(flag);
}
