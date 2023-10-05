using System;

namespace Ktisis.Common.Extensions; 

public static class DelegateEx {
	public static void InvokeSafely(this Delegate @delegate, params object?[]? args) {
		foreach (var invoke in @delegate.GetInvocationList()) {
			try {
				invoke.DynamicInvoke(args);
			} catch (Exception e) {
				Ktisis.Log.Error($"Error in invocation of {@delegate.GetType().Name}:\n{e}");
			}
		}
	}
}
