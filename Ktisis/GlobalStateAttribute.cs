using System;

namespace Ktisis {
	/**
	 * <summary>Indicates that this class has global state and may contain static methods annotated with <see cref="GlobalInitAttribute"/> and/or <see cref="GlobalDisposeAttribute"/>.</summary>
	 */
	[AttributeUsage(AttributeTargets.Class, Inherited = false)]
	public class GlobalStateAttribute : Attribute {}
}
