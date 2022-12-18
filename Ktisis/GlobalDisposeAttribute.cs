using System;

using JetBrains.Annotations;

namespace Ktisis {
	/**
	 * <summary>Marks a static method as a global destructor. It will be called during plugin disposing.</summary>
	 */
	[AttributeUsage(AttributeTargets.Method, Inherited = false)]
	[MeansImplicitUse(ImplicitUseKindFlags.Access)]
	public class GlobalDisposeAttribute : Attribute {}
}
