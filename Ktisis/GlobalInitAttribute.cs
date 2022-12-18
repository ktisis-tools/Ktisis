using System;

using JetBrains.Annotations;

namespace Ktisis {
	/**
	 * <summary>Marks a static method as a global initializer. It will be called during plugin construction.</summary>
	 */
	[AttributeUsage(AttributeTargets.Method, Inherited = false)]
	[MeansImplicitUse(ImplicitUseKindFlags.Access)]
	public class GlobalInitAttribute : Attribute {}
}
